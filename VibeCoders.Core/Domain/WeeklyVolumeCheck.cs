using VibeCoders.Services;

namespace VibeCoders.Domain;

/// <summary>
/// Awards the badge when the client completes at least
/// <see cref="RequiredWorkoutsPerWeek"/> workouts within the last 7 calendar days.
/// </summary>
public sealed class WeeklyVolumeCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }
    public int RequiredWorkoutsPerWeek { get; }

    public WeeklyVolumeCheck(string achievementTitle, int requiredWorkoutsPerWeek)
    {
        AchievementTitle       = achievementTitle;
        RequiredWorkoutsPerWeek = requiredWorkoutsPerWeek;
    }

    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetWorkoutsInLastSevenDays(clientId) >= RequiredWorkoutsPerWeek;
}
