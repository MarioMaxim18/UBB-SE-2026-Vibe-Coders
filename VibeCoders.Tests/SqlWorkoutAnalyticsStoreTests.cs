using Microsoft.Data.Sqlite;
using VibeCoders.Models.Analytics;
using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests;

public sealed class SqlWorkoutAnalyticsStoreTests : IAsyncLifetime
{
    private string _dbPath = null!;
    private string _connectionString = null!;
    private SqlWorkoutAnalyticsStore _store = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"vibecoders_test_{Guid.NewGuid():N}.db");
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
        _connectionString = $"Data Source={_dbPath}";

        var schemaPath = Path.Combine(AppContext.BaseDirectory, "schema.sql");
        Assert.True(File.Exists(schemaPath), $"schema.sql not found at {schemaPath}");

        var schemaSql = await File.ReadAllTextAsync(schemaPath);
        await using (var conn = new SqliteConnection(_connectionString))
        {
            await conn.OpenAsync();
            await using var cmd = new SqliteCommand(schemaSql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        await using (var conn = new SqliteConnection(_connectionString))
        {
            await conn.OpenAsync();
            Seed(conn);
        }

        _store = new SqlWorkoutAnalyticsStore(_connectionString);
    }

    public Task DisposeAsync()
    {
        try
        {
            if (File.Exists(_dbPath)) File.Delete(_dbPath);
        }
        catch
        {
            // ignore locked file on some runners
        }

        return Task.CompletedTask;
    }

    private static void Seed(SqliteConnection conn)
    {
        void Exec(string sql)
        {
            var c = conn.CreateCommand();
            c.CommandText = sql;
            c.ExecuteNonQuery();
        }

        // Trainer user 1, client user 99, client row id 5 (not equal to user id — catches user_id vs client_id mixups)
        Exec(@"INSERT INTO ""USER"" (id, username, password_hash, role) VALUES (1, 'trainer', '', 'TRAINER');");
        Exec("INSERT INTO TRAINER (trainer_id, user_id) VALUES (1, 1);");
        Exec(@"INSERT INTO ""USER"" (id, username, password_hash, role) VALUES (99, 'client', '', 'CLIENT');");
        Exec("INSERT INTO CLIENT (client_id, user_id, trainer_id, weight, height) VALUES (5, 99, 1, 80, 180);");
        Exec("INSERT INTO WORKOUT_TEMPLATE (workout_template_id, client_id, name, type) VALUES (10, 5, 'Morning', 'CUSTOM');");

        Exec("""
            INSERT INTO WORKOUT_LOG (workout_log_id, client_id, workout_id, date, total_duration, calories_burned, rating, intensity_tag)
            VALUES (1, 5, 10, '2026-01-15T10:00:00', '01:00:00', 300, NULL, 'moderate');
            """);

        // Insert order: Squat set 0, Bench set 0, Squat set 1 — rowid order must differ from name order
        Exec("""
            INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note)
            VALUES (1, 'Squat', 0, 10, 100, 10, 100, 1, 0, '');
            """);
        Exec("""
            INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note)
            VALUES (1, 'Bench Press', 0, 8, 60, 8, 60, 1, 0, '');
            """);
        Exec("""
            INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight, target_reps, target_weight, performance_ratio, is_system_adjusted, adjustment_note)
            VALUES (1, 'Squat', 1, 8, 105, 8, 105, 1, 0, '');
            """);
    }

    [Fact]
    public async Task Dashboard_summary_counts_by_client_id_not_user_id()
    {
        var forClient = await _store.GetDashboardSummaryAsync(5);
        Assert.True(forClient.TotalWorkouts >= 1);
        Assert.Equal("Morning", forClient.PreferredWorkoutName);

        // User id 99 owns client 5, but analytics API is client-scoped — passing 99 must not see client 5's rows
        var wrongKey = await _store.GetDashboardSummaryAsync(99);
        Assert.Equal(0, wrongKey.TotalWorkouts);
    }

    [Fact]
    public async Task History_page_filters_by_client_id()
    {
        var page = await _store.GetWorkoutHistoryPageAsync(5, 0, 10);
        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);

        var empty = await _store.GetWorkoutHistoryPageAsync(99, 0, 10);
        Assert.Equal(0, empty.TotalCount);
    }

    [Fact]
    public async Task Session_detail_requires_matching_client_id()
    {
        var ok = await _store.GetWorkoutSessionDetailAsync(5, 1);
        Assert.NotNull(ok);
        Assert.Equal(3, ok!.Sets.Count);

        var wrong = await _store.GetWorkoutSessionDetailAsync(99, 1);
        Assert.Null(wrong);
    }

    [Fact]
    public async Task Session_detail_sets_follow_insertion_order()
    {
        var detail = await _store.GetWorkoutSessionDetailAsync(5, 1);
        Assert.NotNull(detail);
        var names = detail!.Sets.Select(s => s.ExerciseName).ToList();
        Assert.Equal(new[] { "Squat", "Bench Press", "Squat" }, names);
    }
}
