using VibeCoders.Models;

namespace VibeCoders.Services;

/// <summary>
/// Default implementation that persists a completed workout to the
/// analytics SQLite store and then notifies the dashboard refresh bus
/// so any open dashboard view picks up the new data immediately.
/// </summary>
public sealed class WorkoutDataForwarder : IWorkoutDataForwarder
{
    private readonly IWorkoutAnalyticsStore _store;
    private readonly IAnalyticsDashboardRefreshBus _refreshBus;

    public WorkoutDataForwarder(
        IWorkoutAnalyticsStore store,
        IAnalyticsDashboardRefreshBus refreshBus)
    {
        _store = store;
        _refreshBus = refreshBus;
    }

    /// <inheritdoc />
    public async Task<int> ForwardCompletedWorkoutAsync(
        long userId, WorkoutLog log, CancellationToken cancellationToken = default)
    {
        log.TotalCaloriesBurned = log.Exercises.Sum(e => e.ExerciseCaloriesBurned);
        
        if (log.Exercises.Count > 0)
        {
            log.AverageMet = log.Exercises.Average(e => e.Met);
            log.IntensityTag = CalculateIntensityTag(log.AverageMet);
        }

        var logId = await _store.SaveWorkoutAsync(userId, log, cancellationToken);

        _refreshBus.RequestRefresh();

        return logId;
    }

    private static string CalculateIntensityTag(float averageMet)
    {
        if (averageMet < 3.0f)
        {
            return "light";
        }
        else if (averageMet < 6.0f)
        {
            return "moderate";
        }
        else
        {
            return "intense";
        }
    }
}
