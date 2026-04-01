namespace VibeCoders.Models;

/// <summary>
/// One row from the <c>ACHIEVEMENT</c> catalog table — the client-agnostic
/// master definition of a badge. Contains no unlock state; use
/// <see cref="AchievementShowcaseItem"/> when you also need per-client status.
/// </summary>
public sealed class AchievementDefinition
{
    /// <summary>Primary key from <c>ACHIEVEMENT.achievement_id</c>.</summary>
    public int AchievementId { get; init; }

    /// <summary>Short display name of the badge.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Flavor text describing the spirit of the achievement.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Human-readable rule the client must satisfy to earn this badge
    /// (e.g. "Complete 10 total workouts.").
    /// </summary>
    public string Criteria { get; init; } = string.Empty;

    /// <summary>
    /// Workout count threshold that automatically unlocks this badge, or
    /// <see langword="null"/> when the achievement is not a workout-count milestone.
    /// </summary>
    public int? ThresholdWorkouts { get; init; }
}
