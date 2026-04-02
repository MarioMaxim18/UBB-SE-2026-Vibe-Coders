using VibeCoders.Services;

namespace VibeCoders.Domain;

/// <summary>
/// Awards the badge when the client has logged workouts on at least
/// <see cref="RequiredConsecutiveDays"/> consecutive calendar days.
/// </summary>
public sealed class StreakCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }
    public int RequiredConsecutiveDays { get; }

    public StreakCheck(string achievementTitle, int requiredConsecutiveDays)
    {
        AchievementTitle       = achievementTitle;
        RequiredConsecutiveDays = requiredConsecutiveDays;
    }

    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetConsecutiveWorkoutDayStreak(clientId) >= RequiredConsecutiveDays;
}
