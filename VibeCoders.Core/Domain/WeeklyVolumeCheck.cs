using VibeCoders.Services;

namespace VibeCoders.Domain;

/// <summary>
/// Awards the badge when the client completes at least
/// <see cref="RequiredWorkoutsPerWeek"/> workouts within the last 7 calendar days.
/// Covers: "Week Champion" (6 workouts in a week).
/// </summary>
public sealed class WeeklyVolumeCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }

    /// <summary>Minimum workouts required within the last 7 days.</summary>
    public int RequiredWorkoutsPerWeek { get; }

    public WeeklyVolumeCheck(string achievementTitle, int requiredWorkoutsPerWeek)
    {
        AchievementTitle = achievementTitle;
        RequiredWorkoutsPerWeek = requiredWorkoutsPerWeek;
    }

    /// <inheritdoc />
    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetWorkoutsInLastSevenDays(clientId) >= RequiredWorkoutsPerWeek;
}
