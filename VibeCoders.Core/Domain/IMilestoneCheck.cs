using VibeCoders.Services;

namespace VibeCoders.Domain;

/// <summary>
/// Contract for a single achievement criterion evaluated by the
/// <see cref="VibeCoders.Services.EvaluationEngine"/>.
/// Each implementation encapsulates one rule (total count, streak, weekly volume, etc.)
/// and knows the exact achievement title it maps to in the DB catalog.
/// </summary>
public interface IMilestoneCheck
{
    /// <summary>
    /// Must match exactly the <c>ACHIEVEMENT.title</c> value seeded in the database.
    /// Used by the engine to look up the achievement ID before awarding it.
    /// </summary>
    string AchievementTitle { get; }

    /// <summary>
    /// Returns <see langword="true"/> when the client has met the criterion.
    /// Implementations should be read-only — no side effects, no state mutation.
    /// </summary>
    bool IsMet(int clientId, IDataStorage storage);
}
