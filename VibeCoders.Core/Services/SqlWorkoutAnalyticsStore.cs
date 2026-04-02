using System.Globalization;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Models.Analytics;

namespace VibeCoders.Services;

/// <summary>
/// Reads from the shared schema tables created by <see cref="SqlDataStorage.EnsureSchemaCreated"/>.
/// Does NOT create its own tables — schema ownership belongs to SqlDataStorage.
/// All queries scope results to the given user_id via CLIENT join.
/// </summary>
public sealed class SqlWorkoutAnalyticsStore : IWorkoutAnalyticsStore
{
    private readonly string _connectionString;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqlWorkoutAnalyticsStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    // ── Schema ───────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken = default)
    {
        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            // Indexes are already created by SqlDataStorage.EnsureSchemaCreated().
            // Nothing extra to do here for SQLite.
            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<int> SaveWorkoutAsync(
        long userId, WorkoutLog log, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(log);
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var tx = conn.BeginTransaction();

        try
        {
            // Resolve client_id from user_id.
            int clientId;
            await using (var getClient = new SqliteCommand(
                "SELECT client_id FROM CLIENT WHERE user_id = @uid;", conn, tx))
            {
                getClient.Parameters.AddWithValue("@uid", userId);
                var result = await getClient.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (result == null)
                    throw new InvalidOperationException($"No client found for user_id {userId}.");
                clientId = Convert.ToInt32(result);
            }

            int logId;
            await using (var insertLog = new SqliteCommand(@"
                INSERT INTO WORKOUT_LOG
                    (client_id, workout_id, date, total_duration, calories_burned, rating, intensity_tag)
                VALUES
                    (@clientId, @tmpl, @date, @dur, @cal, NULL, @intensity);
                SELECT last_insert_rowid();", conn, tx))
            {
                insertLog.Parameters.AddWithValue("@clientId", clientId);
                insertLog.Parameters.AddWithValue("@tmpl", log.SourceTemplateId);
                insertLog.Parameters.AddWithValue("@date", log.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                insertLog.Parameters.AddWithValue("@dur", log.Duration.ToString());
                insertLog.Parameters.AddWithValue("@cal", log.TotalCaloriesBurned);
                insertLog.Parameters.AddWithValue("@intensity", log.IntensityTag ?? string.Empty);

                logId = Convert.ToInt32(
                    await insertLog.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false),
                    CultureInfo.InvariantCulture);
                log.Id = logId;
            }

            foreach (var exercise in log.Exercises)
            {
                foreach (var set in exercise.Sets)
                {
                    await using var insertSet = new SqliteCommand(@"
                        INSERT INTO WORKOUT_LOG_SETS
                            (workout_log_id, exercise_name, sets, reps, weight,
                             target_reps, target_weight, performance_ratio,
                             is_system_adjusted, adjustment_note)
                        VALUES
                            (@lid, @ex, @si, @ar, @aw, @tr, @tw, @ratio, @adjusted, @note);",
                        conn, tx);

                    insertSet.Parameters.AddWithValue("@lid", logId);
                    insertSet.Parameters.AddWithValue("@ex", exercise.ExerciseName);
                    insertSet.Parameters.AddWithValue("@si", set.SetIndex);
                    insertSet.Parameters.AddWithValue("@ar", (object?)set.ActualReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@aw", (object?)set.ActualWeight ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@tr", (object?)set.TargetReps ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@tw", (object?)set.TargetWeight ?? DBNull.Value);
                    insertSet.Parameters.AddWithValue("@ratio", exercise.PerformanceRatio);
                    insertSet.Parameters.AddWithValue("@adjusted", exercise.IsSystemAdjusted ? 1 : 0);
                    insertSet.Parameters.AddWithValue("@note", exercise.AdjustmentNote);

                    await insertSet.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            await tx.CommitAsync(cancellationToken).ConfigureAwait(false);
            return logId;
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    // ── Dashboard Summary ────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<DashboardSummary> GetDashboardSummaryAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn, @"
            SELECT COUNT(*)
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid;",
            "@uid", userId, cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var windowStart = today.AddDays(-6);

        // SQLite stores duration as text (HH:mm:ss). We sum up by parsing in a subquery
        // using strftime, but the safest approach is to pull durations and sum in C#.
        var activeSeconds = await GetActiveSecondsInRangeAsync(
            conn, userId,
            windowStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            cancellationToken).ConfigureAwait(false);

        string? preferred = null;
        await using (var prefCmd = new SqliteCommand(@"
            SELECT wt.name
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE c.user_id = @uid AND wt.name IS NOT NULL
            GROUP BY wt.name
            ORDER BY COUNT(*) DESC, wt.name ASC
            LIMIT 1;", conn))
        {
            prefCmd.Parameters.AddWithValue("@uid", userId);
            await using var reader = await prefCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                preferred = reader.GetString(0);
        }

        return new DashboardSummary
        {
            TotalWorkouts = (int)Math.Min(int.MaxValue, total),
            TotalActiveTimeLastSevenDays = TimeSpan.FromSeconds(activeSeconds),
            PreferredWorkoutName = preferred
        };
    }

    /// <inheritdoc />
    public async Task<TimeSpan> GetTotalActiveTimeAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var totalSeconds = await GetActiveSecondsInRangeAsync(
            conn, userId,
            null, null,
            cancellationToken).ConfigureAwait(false);

        return TimeSpan.FromSeconds(totalSeconds);
    }

    // ── Consistency ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConsistencyWeekBucket>> GetConsistencyLastFourWeeksAsync(
        long userId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var mondayThisWeek = GetMondayOfWeek(today);
        var buckets = new List<ConsistencyWeekBucket>(4);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        for (var i = 0; i < 4; i++)
        {
            var weekStart = mondayThisWeek.AddDays(-21 + i * 7);
            var weekEnd = weekStart.AddDays(7);

            var count = await ScalarLongAsync(conn, @"
                SELECT COUNT(*)
                FROM WORKOUT_LOG wl
                JOIN CLIENT c ON c.client_id = wl.client_id
                WHERE c.user_id = @uid
                  AND DATE(wl.date) >= @start
                  AND DATE(wl.date) <  @end;",
                "@uid", userId,
                "@start", weekStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                "@end", weekEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                cancellationToken).ConfigureAwait(false);

            buckets.Add(new ConsistencyWeekBucket
            {
                WeekStart = weekStart,
                WorkoutCount = (int)count
            });
        }

        return buckets;
    }

    // ── History Page ─────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<WorkoutHistoryPageResult> GetWorkoutHistoryPageAsync(
        long userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;

        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        var total = await ScalarLongAsync(conn, @"
            SELECT COUNT(*)
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid;",
            "@uid", userId, cancellationToken).ConfigureAwait(false);

        // SQLite uses LIMIT/OFFSET instead of OFFSET x ROWS FETCH NEXT y ROWS ONLY
        await using var cmd = new SqliteCommand(@"
            SELECT
                wl.workout_log_id,
                COALESCE(wt.name, ''),
                wl.date,
                wl.total_duration,
                COALESCE(wl.calories_burned, 0),
                COALESCE(wl.intensity_tag, '')
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE c.user_id = @uid
            ORDER BY wl.date DESC, wl.workout_log_id DESC
            LIMIT @take OFFSET @skip;", conn);

        cmd.Parameters.AddWithValue("@uid", userId);
        cmd.Parameters.AddWithValue("@skip", pageIndex * pageSize);
        cmd.Parameters.AddWithValue("@take", pageSize);

        var items = new List<WorkoutHistoryRow>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            items.Add(new WorkoutHistoryRow
            {
                Id = reader.GetInt32(0),
                WorkoutName = reader.GetString(1),
                LogDate = DateTime.Parse(reader.GetString(2)),
                DurationSeconds = ParseDurationToSeconds(reader.IsDBNull(3) ? null : reader.GetString(3)),
                TotalCaloriesBurned = reader.GetInt32(4),
                IntensityTag = reader.GetString(5)
            });
        }

        return new WorkoutHistoryPageResult
        {
            TotalCount = (int)Math.Min(int.MaxValue, total),
            Items = items
        };
    }

    // ── Session Detail ───────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<WorkoutSessionDetail?> GetWorkoutSessionDetailAsync(
        long userId, int workoutLogId, CancellationToken cancellationToken = default)
    {
        await EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

        string? workoutName;
        DateTime logDate;
        int duration;
        int totalCalories;
        string intensityTag;

        await using (var head = new SqliteCommand(@"
            SELECT
                COALESCE(wt.name, ''),
                wl.date,
                wl.total_duration,
                COALESCE(wl.calories_burned, 0),
                COALESCE(wl.intensity_tag, '')
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            LEFT JOIN WORKOUT_TEMPLATE wt ON wt.workout_template_id = wl.workout_id
            WHERE wl.workout_log_id = @id AND c.user_id = @uid;", conn))
        {
            head.Parameters.AddWithValue("@id", workoutLogId);
            head.Parameters.AddWithValue("@uid", userId);

            await using var r = await head.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await r.ReadAsync(cancellationToken).ConfigureAwait(false)) return null;

            workoutName = r.GetString(0);
            logDate = DateTime.Parse(r.GetString(1));
            duration = ParseDurationToSeconds(r.IsDBNull(2) ? null : r.GetString(2));
            totalCalories = r.GetInt32(3);
            intensityTag = r.GetString(4);
        }

        // Build per-exercise calorie info from sets.
        var exerciseCalories = new List<ExerciseCalorieInfo>();
        var sets = new List<WorkoutSetRow>();

        await using (var setsCmd = new SqliteCommand(@"
            SELECT exercise_name, sets, reps, weight
            FROM WORKOUT_LOG_SETS
            WHERE workout_log_id = @lid
            ORDER BY exercise_name ASC, sets ASC;", conn))
        {
            setsCmd.Parameters.AddWithValue("@lid", workoutLogId);
            await using var sr = await setsCmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var exerciseSetCounts = new Dictionary<string, int>();

            while (await sr.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var exName = sr.GetString(0);
                sets.Add(new WorkoutSetRow
                {
                    ExerciseName = exName,
                    SetIndex = sr.GetInt32(1),
                    ActualReps = sr.IsDBNull(2) ? null : sr.GetInt32(2),
                    ActualWeight = sr.IsDBNull(3) ? null : sr.GetDouble(3)
                });

                exerciseSetCounts.TryGetValue(exName, out var count);
                exerciseSetCounts[exName] = count + 1;
            }

            int totalSets = exerciseSetCounts.Values.Sum();
            foreach (var (exName, setCount) in exerciseSetCounts)
            {
                int calories = totalSets > 0
                    ? (int)Math.Round((double)totalCalories * setCount / totalSets)
                    : 0;

                exerciseCalories.Add(new ExerciseCalorieInfo
                {
                    ExerciseName = exName,
                    CaloriesBurned = calories
                });
            }
        }

        return new WorkoutSessionDetail
        {
            WorkoutLogId = workoutLogId,
            WorkoutName = workoutName,
            LogDate = logDate,
            DurationSeconds = duration,
            TotalCaloriesBurned = totalCalories,
            IntensityTag = intensityTag,
            Sets = sets,
            ExerciseCalories = exerciseCalories
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static int ParseDurationToSeconds(string? duration)
    {
        if (string.IsNullOrWhiteSpace(duration)) return 0;
        if (TimeSpan.TryParse(duration, out var ts)) return (int)ts.TotalSeconds;
        return 0;
    }

    /// <summary>
    /// Sums active time (in seconds) for a user in an optional date range.
    /// Duration is stored as "HH:mm:ss" text — parsed in C# to avoid SQLite time limitations.
    /// </summary>
    private static async Task<long> GetActiveSecondsInRangeAsync(
        SqliteConnection conn,
        long userId,
        string? startDate,
        string? endDate,
        CancellationToken cancellationToken)
    {
        // Pull all relevant total_duration strings and sum them in C#.
        var sql = new System.Text.StringBuilder(@"
            SELECT wl.total_duration
            FROM WORKOUT_LOG wl
            JOIN CLIENT c ON c.client_id = wl.client_id
            WHERE c.user_id = @uid
              AND wl.total_duration IS NOT NULL");

        if (startDate != null)
            sql.Append(" AND DATE(wl.date) >= @start");
        if (endDate != null)
            sql.Append(" AND DATE(wl.date) <= @end");
        sql.Append(';');

        await using var cmd = new SqliteCommand(sql.ToString(), conn);
        cmd.Parameters.AddWithValue("@uid", userId);
        if (startDate != null) cmd.Parameters.AddWithValue("@start", startDate);
        if (endDate   != null) cmd.Parameters.AddWithValue("@end",   endDate);

        long totalSeconds = 0;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            totalSeconds += ParseDurationToSeconds(reader.GetString(0));
        }

        return totalSeconds;
    }

    private static async Task<long> ScalarLongAsync(
        SqliteConnection conn, string sql,
        string paramName, long paramValue,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue(paramName, paramValue);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    private static async Task<long> ScalarLongAsync(
        SqliteConnection conn, string sql,
        string p1, long v1, string p2, string v2, string p3, string v3,
        CancellationToken cancellationToken)
    {
        await using var cmd = new SqliteCommand(sql, conn);
        cmd.Parameters.AddWithValue(p1, v1);
        cmd.Parameters.AddWithValue(p2, v2);
        cmd.Parameters.AddWithValue(p3, v3);
        var obj = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return Convert.ToInt64(obj ?? 0L, CultureInfo.InvariantCulture);
    }

    internal static DateOnly GetMondayOfWeek(DateOnly date)
    {
        var dow = date.DayOfWeek;
        var offset = dow == DayOfWeek.Sunday ? 6 : (int)dow - (int)DayOfWeek.Monday;
        return date.AddDays(-offset);
    }
}