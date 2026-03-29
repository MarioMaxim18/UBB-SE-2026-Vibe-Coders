using Microsoft.Data.Sqlite;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.Tests;

/// <summary>
/// Integration tests for <see cref="SqlWorkoutAnalyticsStore"/> running
/// against an in-memory SQLite database.
/// </summary>
public sealed class SqlWorkoutAnalyticsStoreTests : IAsyncLifetime
{
    private readonly string _connStr;
    private SqliteConnection _keepAlive = null!;
    private SqlWorkoutAnalyticsStore _store = null!;

    public SqlWorkoutAnalyticsStoreTests()
    {
        var dbName = "test_" + Guid.NewGuid().ToString("N");
        _connStr = $"Data Source={dbName};Mode=Memory;Cache=Shared";
    }

    public async Task InitializeAsync()
    {
        _keepAlive = new SqliteConnection(_connStr);
        _keepAlive.Open();
        _store = new SqlWorkoutAnalyticsStore(_connStr, raw: true);
        await _store.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _keepAlive.Dispose();
        return Task.CompletedTask;
    }

    // -- Helpers --

    private async Task InsertLog(long userId, string name, string date, int durationSec)
    {
        await using var conn = new SqliteConnection(_connStr);
        conn.Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO workout_log (user_id, workout_name, log_date, duration_seconds)
            VALUES ($uid, $name, $date, $dur);";
        cmd.Parameters.AddWithValue("$uid", userId);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$dur", durationSec);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertLogReturningId(long userId, string name, string date, int durationSec)
    {
        await using var conn = new SqliteConnection(_connStr);
        conn.Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO workout_log (user_id, workout_name, log_date, duration_seconds)
            VALUES ($uid, $name, $date, $dur)
            RETURNING id;";
        cmd.Parameters.AddWithValue("$uid", userId);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.Parameters.AddWithValue("$date", date);
        cmd.Parameters.AddWithValue("$dur", durationSec);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task InsertSet(int logId, string exercise, int setIdx, int? reps, double? weight)
    {
        await using var conn = new SqliteConnection(_connStr);
        conn.Open();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO workout_log_sets
                (workout_log_id, exercise_name, set_index, actual_reps, actual_weight)
            VALUES ($lid, $ex, $si, $reps, $wt);";
        cmd.Parameters.AddWithValue("$lid", logId);
        cmd.Parameters.AddWithValue("$ex", exercise);
        cmd.Parameters.AddWithValue("$si", setIdx);
        cmd.Parameters.AddWithValue("$reps", (object?)reps ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$wt", (object?)weight ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    // -- Empty state --

    [Fact]
    public async Task Summary_returns_zeros_when_no_data()
    {
        var s = await _store.GetDashboardSummaryAsync(999);
        Assert.Equal(0, s.TotalWorkouts);
        Assert.Equal(TimeSpan.Zero, s.TotalActiveTimeLastSevenDays);
        Assert.Null(s.PreferredWorkoutName);
    }

    [Fact]
    public async Task History_returns_empty_page_when_no_data()
    {
        var page = await _store.GetWorkoutHistoryPageAsync(999, 0, 10);
        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task Consistency_returns_four_buckets_when_no_data()
    {
        var buckets = await _store.GetConsistencyLastFourWeeksAsync(999);
        Assert.Equal(4, buckets.Count);
        Assert.All(buckets, b => Assert.Equal(0, b.WorkoutCount));
    }

    [Fact]
    public async Task SessionDetail_returns_null_for_missing_log()
    {
        Assert.Null(await _store.GetWorkoutSessionDetailAsync(1, 999999));
    }

    // -- Total workouts --

    [Fact]
    public async Task TotalWorkouts_counts_only_for_given_user()
    {
        await InsertLog(1, "Push", "2026-03-01", 600);
        await InsertLog(1, "Pull", "2026-03-02", 600);
        await InsertLog(2, "Legs", "2026-03-02", 600);

        var s1 = await _store.GetDashboardSummaryAsync(1);
        Assert.Equal(2, s1.TotalWorkouts);

        var s2 = await _store.GetDashboardSummaryAsync(2);
        Assert.Equal(1, s2.TotalWorkouts);
    }

    // -- Preferred workout --

    [Fact]
    public async Task PreferredWorkout_returns_most_frequent()
    {
        await InsertLog(1, "Push", "2026-03-01", 600);
        await InsertLog(1, "Pull", "2026-03-02", 600);
        await InsertLog(1, "Push", "2026-03-03", 600);

        var s = await _store.GetDashboardSummaryAsync(1);
        Assert.Equal("Push", s.PreferredWorkoutName);
    }

    [Fact]
    public async Task PreferredWorkout_breaks_ties_alphabetically()
    {
        await InsertLog(1, "Zebra", "2026-03-01", 600);
        await InsertLog(1, "Alpha", "2026-03-02", 600);

        var s = await _store.GetDashboardSummaryAsync(1);
        Assert.Equal("Alpha", s.PreferredWorkoutName);
    }

    // -- Pagination --

    [Fact]
    public async Task Pagination_returns_correct_pages()
    {
        for (var i = 1; i <= 5; i++)
        {
            await InsertLog(1, $"W{i}", $"2026-03-{i:D2}", 600);
        }

        var p0 = await _store.GetWorkoutHistoryPageAsync(1, 0, 2);
        Assert.Equal(5, p0.TotalCount);
        Assert.Equal(2, p0.Items.Count);
        Assert.Equal("W5", p0.Items[0].WorkoutName);
        Assert.Equal("W4", p0.Items[1].WorkoutName);

        var p2 = await _store.GetWorkoutHistoryPageAsync(1, 2, 2);
        Assert.Single(p2.Items);
        Assert.Equal("W1", p2.Items[0].WorkoutName);
    }

    [Fact]
    public async Task Pagination_clamps_negative_page_index()
    {
        await InsertLog(1, "W1", "2026-03-01", 600);
        var page = await _store.GetWorkoutHistoryPageAsync(1, -1, 10);
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
    }

    // -- Session detail --

    [Fact]
    public async Task SessionDetail_returns_sets_for_valid_log()
    {
        var logId = await InsertLogReturningId(1, "Push", "2026-03-01", 3600);
        await InsertSet(logId, "Bench Press", 0, 8, 60.0);
        await InsertSet(logId, "Bench Press", 1, 7, 60.0);

        var detail = await _store.GetWorkoutSessionDetailAsync(1, logId);
        Assert.NotNull(detail);
        Assert.Equal("Push", detail!.WorkoutName);
        Assert.Equal(2, detail.Sets.Count);
        Assert.Equal("Bench Press", detail.Sets[0].ExerciseName);
        Assert.Equal(8, detail.Sets[0].ActualReps);
    }

    [Fact]
    public async Task SessionDetail_returns_null_for_wrong_user()
    {
        var logId = await InsertLogReturningId(1, "Push", "2026-03-01", 3600);
        Assert.Null(await _store.GetWorkoutSessionDetailAsync(2, logId));
    }

    // -- SaveWorkoutAsync --

    [Fact]
    public async Task SaveWorkout_inserts_log_and_sets()
    {
        var log = new WorkoutLog
        {
            WorkoutName = "Full Body",
            Date = new DateTime(2026, 3, 20),
            Duration = TimeSpan.FromMinutes(45),
            SourceTemplateId = 7,
            Exercises = new List<LoggedExercise>
            {
                new()
                {
                    ExerciseName = "Squat",
                    Sets = new List<LoggedSet>
                    {
                        new() { SetIndex = 0, ActualReps = 10, ActualWeight = 80.0, TargetReps = 12, TargetWeight = 80.0 },
                        new() { SetIndex = 1, ActualReps = 8, ActualWeight = 85.0 },
                    }
                },
                new()
                {
                    ExerciseName = "Bench Press",
                    Sets = new List<LoggedSet>
                    {
                        new() { SetIndex = 0, ActualReps = 10, ActualWeight = 60.0 },
                    }
                }
            }
        };

        var logId = await _store.SaveWorkoutAsync(1, log);
        Assert.True(logId > 0);

        var detail = await _store.GetWorkoutSessionDetailAsync(1, logId);
        Assert.NotNull(detail);
        Assert.Equal("Full Body", detail!.WorkoutName);
        Assert.Equal(2700, detail.DurationSeconds);
        Assert.Equal(3, detail.Sets.Count);
    }

    [Fact]
    public async Task SaveWorkout_updates_dashboard_summary()
    {
        var log = new WorkoutLog
        {
            WorkoutName = "Legs",
            Date = DateTime.Today,
            Duration = TimeSpan.FromMinutes(30),
            Exercises = new List<LoggedExercise>()
        };

        await _store.SaveWorkoutAsync(1, log);
        var summary = await _store.GetDashboardSummaryAsync(1);
        Assert.Equal(1, summary.TotalWorkouts);
        Assert.Equal("Legs", summary.PreferredWorkoutName);
    }

    [Fact]
    public async Task SaveWorkout_preserves_nullable_set_fields()
    {
        var log = new WorkoutLog
        {
            WorkoutName = "Arms",
            Date = new DateTime(2026, 3, 15),
            Duration = TimeSpan.FromMinutes(20),
            Exercises = new List<LoggedExercise>
            {
                new()
                {
                    ExerciseName = "Curl",
                    Sets = new List<LoggedSet>
                    {
                        new() { SetIndex = 0, ActualReps = null, ActualWeight = null }
                    }
                }
            }
        };

        var logId = await _store.SaveWorkoutAsync(1, log);
        var detail = await _store.GetWorkoutSessionDetailAsync(1, logId);
        Assert.NotNull(detail);
        Assert.Single(detail!.Sets);
        Assert.Null(detail.Sets[0].ActualReps);
        Assert.Null(detail.Sets[0].ActualWeight);
    }

    // -- Monday calculation helper --

    [Theory]
    [InlineData("2026-03-23", "2026-03-23")] // Monday stays Monday
    [InlineData("2026-03-24", "2026-03-23")] // Tuesday -> Monday
    [InlineData("2026-03-28", "2026-03-23")] // Saturday -> Monday
    [InlineData("2026-03-29", "2026-03-23")] // Sunday -> previous Monday
    public void GetMondayOfWeek_returns_correct_monday(string input, string expected)
    {
        var date = DateOnly.ParseExact(input, "yyyy-MM-dd");
        var expectedDate = DateOnly.ParseExact(expected, "yyyy-MM-dd");
        Assert.Equal(expectedDate, SqlWorkoutAnalyticsStore.GetMondayOfWeek(date));
    }
}
