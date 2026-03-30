namespace VibeCoders.Models;

/// <summary>
/// One achievement row returned for a client who has unlocked it
/// (<see cref="Services.IDataStorage.GetEarnedAchievements"/>).
/// </summary>
public sealed class EarnedAchievement
{
    public int AchievementId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;
}
