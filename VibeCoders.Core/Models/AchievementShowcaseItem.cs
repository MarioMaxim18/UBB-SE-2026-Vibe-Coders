namespace VibeCoders.Models;

/// <summary>
/// One achievement in the rank showcase: catalog metadata plus whether the
/// client has unlocked it. Locked rows stay listed so goals stay visible.
/// </summary>
public sealed class AchievementShowcaseItem
{
    public int AchievementId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsUnlocked { get; init; }

    /// <summary>Short status line for the showcase row (no XAML converters required).</summary>
    public string StatusLine => IsUnlocked ? "Unlocked" : "Locked";
}
