using System.Diagnostics;
using VibeCoders.Domain;
using VibeCoders.Models;

namespace VibeCoders.Services;

/// <summary>
/// Core background evaluation engine for achievements.
/// After every finalized workout call <see cref="Evaluate"/> with the client ID;
/// the engine iterates all registered <see cref="IMilestoneCheck"/> rules, looks up
/// each matching achievement by title, and awards it exactly once per user account.
///
/// Adding a new achievement type:
///   1. Seed a row in <c>ACHIEVEMENT</c> (via <c>SeedWorkoutMilestoneAchievements</c> or a new seed method).
///   2. Implement <see cref="IMilestoneCheck"/> (e.g. <see cref="WorkoutCountCheck"/>, <see cref="StreakCheck"/>).
///   3. Register it in the <c>_checks</c> list below — no other changes needed.
/// </summary>
public sealed class EvaluationEngine
{
    private readonly IDataStorage _storage;
    private readonly IReadOnlyList<IMilestoneCheck> _checks;

    public EvaluationEngine(IDataStorage storage) : this(storage, BuildDefaultChecks()) { }

    /// <summary>
    /// Allows injecting a custom check list (used in unit tests).
    /// </summary>
    public EvaluationEngine(IDataStorage storage, IReadOnlyList<IMilestoneCheck> checks)
    {
        _storage = storage;
        _checks  = checks;
    }

    // ── Default milestone registry ───────────────────────────────────────────

    private static IReadOnlyList<IMilestoneCheck> BuildDefaultChecks() =>
    [
        // ── Total-workout volume milestones ──────────────────────────────────
        // Titles must match ACHIEVEMENT.title rows seeded by SeedWorkoutMilestoneAchievements.
        new WorkoutCountCheck("First Steps",     threshold: 1),
        new WorkoutCountCheck("Deca Athlete",    threshold: 10),
        new WorkoutCountCheck("Quarter Century", threshold: 25),
        new WorkoutCountCheck("Half Century",    threshold: 50),
        new WorkoutCountCheck("Centurion",       threshold: 100),

        // ── Consecutive-day streak milestones ────────────────────────────────
        new StreakCheck("3-Day Streak",  requiredConsecutiveDays: 3),
        new StreakCheck("Week Warrior",  requiredConsecutiveDays: 7),

        // ── Weekly-volume milestone ──────────────────────────────────────────
        new WeeklyVolumeCheck("Week Champion", requiredWorkoutsPerWeek: 6),
    ];

    // ── Public entry point ───────────────────────────────────────────────────

    /// <summary>
    /// Runs every registered milestone check for <paramref name="clientId"/>.
    /// For each check that passes and whose achievement is not yet unlocked,
    /// the achievement is awarded and its title added to the returned list.
    /// The list is empty when nothing new was earned.
    /// </summary>
    /// <param name="clientId">The client to evaluate.</param>
    /// <returns>Titles of achievements newly unlocked in this call.</returns>
    public IReadOnlyList<string> Evaluate(int clientId)
    {
        var newlyUnlocked = new List<string>();

        try
        {
            // Load the full catalog once — avoids N+1 DB hits inside the loop.
            var catalog = _storage
                .GetAchievementShowcaseForClient(clientId)
                .ToDictionary(a => a.Title, StringComparer.OrdinalIgnoreCase);

            foreach (var check in _checks)
            {
                // Skip if the achievement doesn't exist in the catalog yet
                // (e.g. seed hasn't run) or is already unlocked.
                if (!catalog.TryGetValue(check.AchievementTitle, out var item)) continue;
                if (item.IsUnlocked) continue;

                if (!check.IsMet(clientId, _storage)) continue;

                bool awarded = _storage.AwardAchievement(clientId, item.AchievementId);
                if (!awarded) continue;

                newlyUnlocked.Add(check.AchievementTitle);
                Debug.WriteLine(
                    $"[EvaluationEngine] Unlocked '{check.AchievementTitle}' for client {clientId}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[EvaluationEngine] Evaluation error for client {clientId}: {ex.Message}");
        }

        return newlyUnlocked;
    }
}
