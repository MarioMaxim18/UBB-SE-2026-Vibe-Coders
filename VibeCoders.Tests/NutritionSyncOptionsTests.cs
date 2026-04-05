using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests;

public sealed class NutritionSyncOptionsTests
{
    [Fact]
    public void Default_endpoint_points_at_localhost_for_dev_api()
    {
        var options = new NutritionSyncOptions();
        Assert.Contains("127.0.0.1", options.Endpoint, StringComparison.Ordinal);
        Assert.EndsWith("/api/nutrition/sync", options.Endpoint, StringComparison.Ordinal);
    }

    [Fact]
    public void UseInProcessMock_defaults_false_so_release_behavior_stays_real_http()
    {
        var options = new NutritionSyncOptions();
        Assert.False(options.UseInProcessMock);
    }
}
