using VibeCoders.Models;

namespace VibeCoders.Tests;

public sealed class EarnedAchievementModelTests
{
    [Fact]
    public void EarnedAchievement_round_trips_properties()
    {
        var row = new EarnedAchievement
        {
            AchievementId = 42,
            Title = "First Steps",
            Description = "Complete your first workout."
        };

        Assert.Equal(42, row.AchievementId);
        Assert.Equal("First Steps", row.Title);
        Assert.Equal("Complete your first workout.", row.Description);
    }
}
