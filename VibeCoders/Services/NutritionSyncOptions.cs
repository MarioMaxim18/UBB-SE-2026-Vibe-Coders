namespace VibeCoders.Services;

/// <summary>
/// Override by registering a different instance in DI, or extend later with appsettings.
/// </summary>
public sealed class NutritionSyncOptions
{
    public string Endpoint { get; set; } = "http://127.0.0.1:5088/api/nutrition/sync";

    /// <summary>
    /// When true, sync succeeds without HTTP (for demos when the other team's API is not available).
    /// </summary>
    public bool UseInProcessMock { get; set; }
}
