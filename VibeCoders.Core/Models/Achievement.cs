namespace VibeCoders.Models;

/// <summary>
/// A badge definition from the <c>ACHIEVEMENT</c> catalog table.
/// <see cref="IsUnlocked"/> reflects a specific client's state when the object
/// is loaded via a client-scoped query; it defaults to <see langword="false"/>
/// when loaded from the client-agnostic <c>GetAllAchievements</c> query.
/// </summary>
public class Achievement
{
    /// <summary>Primary key from <c>ACHIEVEMENT.achievement_id</c>.</summary>
    public int AchievementId { get; set; }

    /// <summary>Short display name of the badge (maps to <c>ACHIEVEMENT.title</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional icon identifier used in the UI.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Flavor text describing the spirit of the achievement.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable rule the client must satisfy to earn this badge
    /// (e.g. "Complete 10 total workouts.").
    /// Maps to <c>ACHIEVEMENT.criteria</c>.
    /// </summary>
    public string Criteria { get; set; } = string.Empty;

    /// <summary>
    /// Workout count that automatically unlocks this badge, or
    /// <see langword="null"/> when the achievement is not a workout-count milestone.
    /// Maps to <c>ACHIEVEMENT.threshold_workouts</c>.
    /// </summary>
    public int? ThresholdWorkouts { get; set; }

    /// <summary>
    /// <see langword="true"/> when this badge has been earned by the current client.
    /// Always <see langword="false"/> when loaded from the master catalog query.
    /// </summary>
    public bool IsUnlocked { get; set; }
}
