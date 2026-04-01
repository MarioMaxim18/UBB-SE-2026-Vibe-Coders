using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.Tests;

/// <summary>
/// Unit tests for <see cref="EvaluationEngine"/>.
/// All tests use a fake <see cref="IDataStorage"/> stub — no DB, no DI.
/// </summary>
public sealed class EvaluationEngineTests
{
    // ── Stub ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Minimal in-memory stub.  Each test configures only the fields it needs.
    /// </summary>
    private sealed class StubStorage : IDataStorage
    {
        public int WorkoutCount { get; set; }
        public int ConsecutiveStreak { get; set; }
        public int WorkoutsLastSevenDays { get; set; }

        /// <summary>Pre-populated catalog returned by GetAchievementShowcaseForClient.</summary>
        public List<AchievementShowcaseItem> Catalog { get; } = [];

        /// <summary>Tracks AwardAchievement calls: (clientId, achievementId).</summary>
        public List<(int ClientId, int AchievementId)> Awarded { get; } = [];

        /// <summary>When true, AwardAchievement returns false (idempotency guard).</summary>
        public bool AwardReturnsFalse { get; set; }

        public int GetWorkoutCount(int clientId) => WorkoutCount;
        public int GetConsecutiveWorkoutDayStreak(int clientId) => ConsecutiveStreak;
        public int GetWorkoutsInLastSevenDays(int clientId) => WorkoutsLastSevenDays;
        public List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId) => Catalog;

        public bool AwardAchievement(int clientId, int achievementId)
        {
            if (AwardReturnsFalse) return false;
            Awarded.Add((clientId, achievementId));
            return true;
        }

        // ── Unused interface members ─────────────────────────────────────────
        public void EnsureSchemaCreated() { }
        public bool SaveUser(User u) => true;
        public User? LoadUser(string username) => null;
        public bool SaveClientData(Client c) => true;
        public List<Client> GetTrainerClient(int trainerId) => [];
        public List<WorkoutTemplate> GetAvailableWorkouts(int clientId) => [];
        public bool SaveWorkoutLog(WorkoutLog log) => true;
        public List<WorkoutLog> GetWorkoutHistory(int clientId) => [];
        public List<WorkoutLog> GetLastTwoLogsForExercise(int id) => [];
        public TemplateExercise? GetTemplateExercise(int id) => null;
        public bool UpdateTemplateWeight(int id, double w) => true;
        public bool SaveNotification(Notification n) => true;
        public List<Notification> GetNotifications(int clientId) => [];
        public int GetDistinctWorkoutDayCount(int clientId) => 0;
        public AchievementShowcaseItem? GetAchievementForClient(int achievementId, int clientId) => null;
        public bool UpdateWorkoutLogFeedback(int workoutLogId, double rating, string notes) => true;
        public void EvaluateAndUnlockWorkoutMilestones(int clientId) { }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AchievementShowcaseItem MakeItem(
        int id, string title, bool isUnlocked = false)
        => new()
        {
            AchievementId = id,
            Title         = title,
            Description   = string.Empty,
            Criteria      = string.Empty,
            IsUnlocked    = isUnlocked,
        };

    // ── No unlocks ───────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_returns_empty_when_no_checks_are_met()
    {
        var storage = new StubStorage { WorkoutCount = 0 };
        storage.Catalog.Add(MakeItem(1, "First Steps", isUnlocked: false));

        var checks = new List<IMilestoneCheck>
        {
            new WorkoutCountCheck("First Steps", threshold: 1),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 42);

        Assert.Empty(result);
        Assert.Empty(storage.Awarded);
    }

    [Fact]
    public void Evaluate_returns_empty_when_achievement_already_unlocked()
    {
        var storage = new StubStorage { WorkoutCount = 5 };
        storage.Catalog.Add(MakeItem(1, "First Steps", isUnlocked: true));  // already done

        var checks = new List<IMilestoneCheck>
        {
            new WorkoutCountCheck("First Steps", threshold: 1),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 42);

        Assert.Empty(result);
        Assert.Empty(storage.Awarded);
    }

    [Fact]
    public void Evaluate_skips_check_whose_title_is_not_in_catalog()
    {
        var storage = new StubStorage { WorkoutCount = 99 };
        // Catalog is intentionally empty — achievement not seeded yet.

        var checks = new List<IMilestoneCheck>
        {
            new WorkoutCountCheck("Ghost Badge", threshold: 1),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 42);

        Assert.Empty(result);
        Assert.Empty(storage.Awarded);
    }

    // ── Single unlock ─────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_unlocks_single_earned_achievement()
    {
        var storage = new StubStorage { WorkoutCount = 1 };
        storage.Catalog.Add(MakeItem(7, "First Steps", isUnlocked: false));

        var checks = new List<IMilestoneCheck>
        {
            new WorkoutCountCheck("First Steps", threshold: 1),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 42);

        Assert.Single(result);
        Assert.Equal("First Steps", result[0]);
        Assert.Single(storage.Awarded);
        Assert.Equal((42, 7), storage.Awarded[0]);
    }

    // ── Multiple checks, partial unlock ──────────────────────────────────────

    [Fact]
    public void Evaluate_unlocks_only_checks_whose_threshold_is_reached()
    {
        var storage = new StubStorage { WorkoutCount = 10 };
        storage.Catalog.Add(MakeItem(1, "First Steps",  isUnlocked: true));   // already unlocked
        storage.Catalog.Add(MakeItem(2, "Deca Athlete", isUnlocked: false));
        storage.Catalog.Add(MakeItem(3, "Centurion",    isUnlocked: false));  // not reached

        var checks = new List<IMilestoneCheck>
        {
            new WorkoutCountCheck("First Steps",  threshold: 1),
            new WorkoutCountCheck("Deca Athlete", threshold: 10),
            new WorkoutCountCheck("Centurion",    threshold: 100),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 1);

        Assert.Single(result);
        Assert.Equal("Deca Athlete", result[0]);
        Assert.Single(storage.Awarded);
        Assert.Equal((1, 2), storage.Awarded[0]);
    }

    // ── StreakCheck ───────────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_unlocks_streak_badge_when_streak_met()
    {
        var storage = new StubStorage { ConsecutiveStreak = 7 };
        storage.Catalog.Add(MakeItem(5, "Week Warrior", isUnlocked: false));

        var checks = new List<IMilestoneCheck>
        {
            new StreakCheck("Week Warrior", requiredConsecutiveDays: 7),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 3);

        Assert.Single(result);
        Assert.Equal("Week Warrior", result[0]);
    }

    [Fact]
    public void Evaluate_does_not_unlock_streak_badge_when_streak_short()
    {
        var storage = new StubStorage { ConsecutiveStreak = 6 };
        storage.Catalog.Add(MakeItem(5, "Week Warrior", isUnlocked: false));

        var checks = new List<IMilestoneCheck>
        {
            new StreakCheck("Week Warrior", requiredConsecutiveDays: 7),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 3);

        Assert.Empty(result);
    }

    // ── WeeklyVolumeCheck ────────────────────────────────────────────────────

    [Fact]
    public void Evaluate_unlocks_weekly_volume_badge_when_threshold_met()
    {
        var storage = new StubStorage { WorkoutsLastSevenDays = 6 };
        storage.Catalog.Add(MakeItem(9, "Week Champion", isUnlocked: false));

        var checks = new List<IMilestoneCheck>
        {
            new WeeklyVolumeCheck("Week Champion", requiredWorkoutsPerWeek: 6),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 7);

        Assert.Single(result);
        Assert.Equal("Week Champion", result[0]);
    }

    // ── AwardAchievement returns false (idempotency guard) ───────────────────

    [Fact]
    public void Evaluate_excludes_achievement_when_AwardAchievement_returns_false()
    {
        var storage = new StubStorage { WorkoutCount = 10, AwardReturnsFalse = true };
        storage.Catalog.Add(MakeItem(2, "Deca Athlete", isUnlocked: false));

        var checks = new List<IMilestoneCheck>
        {
            new WorkoutCountCheck("Deca Athlete", threshold: 10),
        };
        var engine = new EvaluationEngine(storage, checks);

        var result = engine.Evaluate(clientId: 1);

        Assert.Empty(result);    // not reported as newly unlocked
        Assert.Empty(storage.Awarded);
    }

    // ── ClientId propagated correctly ────────────────────────────────────────

    [Fact]
    public void Evaluate_passes_correct_clientId_to_AwardAchievement()
    {
        var storage = new StubStorage { WorkoutCount = 1 };
        storage.Catalog.Add(MakeItem(3, "First Steps", isUnlocked: false));

        var checks = new List<IMilestoneCheck>
        {
            new WorkoutCountCheck("First Steps", threshold: 1),
        };
        var engine = new EvaluationEngine(storage, checks);

        engine.Evaluate(clientId: 99);

        Assert.Equal(99, storage.Awarded[0].ClientId);
    }
}
