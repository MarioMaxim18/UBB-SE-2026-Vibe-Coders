using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

/// <summary>
/// Presents the client's level and rank derived from lifetime active time and
/// lists achievements unlocked in <see cref="IDataStorage.GetEarnedAchievements"/>.
/// Until login maps users to clients, <see cref="IUserSession.CurrentUserId"/> is
/// treated as the client id for the achievements query.
/// </summary>
public sealed partial class RankShowcaseViewModel : ObservableObject
{
    private readonly IWorkoutAnalyticsStore _analytics;
    private readonly IUserSession _session;
    private readonly IDataStorage _data;

    public RankShowcaseViewModel(
        IWorkoutAnalyticsStore analytics,
        IUserSession session,
        IDataStorage data)
    {
        _analytics = analytics;
        _session = session;
        _data = data;
    }

    [ObservableProperty]
    private int displayLevel;

    [ObservableProperty]
    private string rankTitle = "\u2014";

    [ObservableProperty]
    private string totalActiveTimeDisplay = "0:00:00";

    [ObservableProperty]
    private bool isLoading;

    public ObservableCollection<EarnedAchievement> EarnedAchievements { get; } = new();

    /// <summary>
    /// Loads level, rank, lifetime time, and earned achievements from storage.
    /// </summary>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        try
        {
            var userId = _session.CurrentUserId;
            var total = await _analytics.GetTotalActiveTimeAsync(userId, cancellationToken)
                .ConfigureAwait(true);

            var leveling = LevelingTierEvaluator.Evaluate(total);
            DisplayLevel = leveling.Level;
            RankTitle = leveling.RankTitle;
            TotalActiveTimeDisplay = ActiveTimeFormatter.ToHourMinuteSecond(total);

            EarnedAchievements.Clear();
            var clientId = (int)userId;
            foreach (var row in _data.GetEarnedAchievements(clientId))
            {
                EarnedAchievements.Add(row);
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(CancellationToken.None);
}
