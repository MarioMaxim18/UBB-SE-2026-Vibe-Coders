namespace VibeCoders.Services;

/// <summary>
/// Application-level navigation abstraction.
/// </summary>
public interface INavigationService
{
    void NavigateToClientDashboard(bool requestRefresh);
    void NavigateToCalendarIntegration();
    void NavigateToRankShowcase();
    void NavigateToActiveWorkout();
    void NavigateToWorkoutLogs();
    void GoBack();
}