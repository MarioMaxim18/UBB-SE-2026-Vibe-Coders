using VibeCoders.Services;

namespace VibeCoders.Domain;

/// <summary>
/// Awards the badge when the client's lifetime completed workout count
/// reaches or exceeds <see cref="Threshold"/>.
/// Covers: "First Steps" (1), "Deca Athlete" (10), "Quarter Century" (25),
///         "Half Century" (50), "Centurion" (100).
/// </summary>
public sealed class WorkoutCountCheck : IMilestoneCheck
{
    public string AchievementTitle { get; }

    /// <summary>Minimum lifetime workouts required to earn the badge.</summary>
    public int Threshold { get; }

    public WorkoutCountCheck(string achievementTitle, int threshold)
    {
        AchievementTitle = achievementTitle;
        Threshold = threshold;
    }

    /// <inheritdoc />
    public bool IsMet(int clientId, IDataStorage storage)
        => storage.GetWorkoutCount(clientId) >= Threshold;
}
