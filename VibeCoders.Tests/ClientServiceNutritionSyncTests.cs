using System.Net;
using System.Net.Http;
using Moq;
using VibeCoders.Models.Integration;
using VibeCoders.Services;
using Xunit;

namespace VibeCoders.Tests;

public sealed class ClientServiceNutritionSyncTests
{
    [Fact]
    public async Task SyncNutritionAsync_skips_http_when_in_process_mock_enabled()
    {
        var storage = new Mock<IDataStorage>(MockBehavior.Loose);
        var progression = new ProgressionService(storage.Object);
        var httpFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var engine = new EvaluationEngine(storage.Object);
        var bus = new Mock<IAchievementUnlockedBus>(MockBehavior.Loose);
        var options = new NutritionSyncOptions { UseInProcessMock = true };

        var svc = new ClientService(
            storage.Object,
            progression,
            httpFactory.Object,
            engine,
            bus.Object,
            options);

        var ok = await svc.SyncNutritionAsync(new NutritionSyncPayload
        {
            TotalCalories = 100,
            WorkoutDifficulty = "light",
            UserBmi = 22.5f
        });

        Assert.True(ok);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SyncNutritionAsync_uses_http_when_mock_disabled()
    {
        var storage = new Mock<IDataStorage>(MockBehavior.Loose);
        var progression = new ProgressionService(storage.Object);
        var handler = new OkHandler();
        var client = new HttpClient(handler);
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);
        var engine = new EvaluationEngine(storage.Object);
        var bus = new Mock<IAchievementUnlockedBus>(MockBehavior.Loose);
        var options = new NutritionSyncOptions
        {
            UseInProcessMock = false,
            Endpoint = "https://example.invalid/api/nutrition/sync"
        };

        var svc = new ClientService(
            storage.Object,
            progression,
            httpFactory.Object,
            engine,
            bus.Object,
            options);

        var ok = await svc.SyncNutritionAsync(new NutritionSyncPayload
        {
            TotalCalories = 50,
            WorkoutDifficulty = "moderate",
            UserBmi = 24f
        });

        Assert.True(ok);
        httpFactory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
    }

    private sealed class OkHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }
}
