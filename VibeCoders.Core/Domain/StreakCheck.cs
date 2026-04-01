using VibeCoders.Services;

namespace VibeCoders.Domain;

/// <summary>
/// Awards the badge when the client has logged workouts on at least
/// <see cref="RequiredConsecutiveDays"/> consecutive calendar days.
/// Covers: "3-Day Streak" (3), "Week Warrior" (7).
/// </summary>
public sealed class StreakCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }

    /// <summary>Minimum number of consecutive days with at least one workout logged.</summary>
    public int RequiredConsecutiveDays { get; }

    public StreakCheck(string achievementTitle, int requiredConsecutiveDays)
    {
        AchievementTitle = achievementTitle;
        RequiredConsecutiveDays = requiredConsecutiveDays;
    }

    /// <inheritdoc />
    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetConsecutiveWorkoutDayStreak(clientId) >= RequiredConsecutiveDays;
}
