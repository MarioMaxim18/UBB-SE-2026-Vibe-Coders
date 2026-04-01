using VibeCoders.Services;

namespace VibeCoders.Domain;

/// <summary>
/// Contract for a single achievement criterion evaluated by
/// <see cref="VibeCoders.Services.EvaluationEngine"/>.
/// Implementations are read-only — no side effects, no state mutation.
/// </summary>
public interface IMilestoneCheck
{
    /// <summary>
    /// Must match exactly the <c>ACHIEVEMENT.title</c> value seeded in the DB.
    /// Used by the engine to look up the achievement ID before awarding it.
    /// </summary>
    string AchievementTitle { get; }

    /// <summary>Returns <see langword="true"/> when the client has met the criterion.</summary>
    bool IsMet(int clientId, IDataStorage storage);
}
