using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests;

public sealed class WorkoutUiStateTests
{
    [Fact]
    public void ProgressionHeadsUp_round_trips_for_dashboard_banner()
    {
        var state = new WorkoutUiState();
        state.ProgressionHeadsUp = "Bench: +2.5kg";
        Assert.Equal("Bench: +2.5kg", state.ProgressionHeadsUp);
        state.ProgressionHeadsUp = null;
        Assert.Null(state.ProgressionHeadsUp);
    }
}
