using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Domain;
using VibeCoders.Models;
using VibeCoders.Models.Integration;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public partial class ClientProfileViewModel : ObservableObject
{
    private readonly IDataStorage _storage;
    private readonly ClientService _clientService;
    private readonly NutritionSyncOptions _nutritionSyncOptions;
    private int _loadedClientId;

    [ObservableProperty]
    private ObservableCollection<LoggedExercise> loggedExercises = new();

    [ObservableProperty]
    private ObservableCollection<Meal> meals = new();

    [ObservableProperty]
    private string caloriesSummary = "Calories burned (all logged workouts): 0";

    [ObservableProperty]
    private string latestSessionHint = string.Empty;

    [ObservableProperty]
    private string syncNutritionStatus = string.Empty;

    public ClientProfileViewModel(
        IDataStorage storage,
        ClientService clientService,
        NutritionSyncOptions nutritionSyncOptions)
    {
        _storage = storage;
        _clientService = clientService;
        _nutritionSyncOptions = nutritionSyncOptions;
    }

    [RelayCommand]
    private async Task SyncNutritionAsync()
    {
        if (_loadedClientId <= 0) return;

        SyncNutritionStatus = "Syncing…";

        var history = _storage.GetWorkoutHistory(_loadedClientId);
        var totalCalories = history.Sum(h => h.TotalCaloriesBurned);
        var last = history.FirstOrDefault();
        var difficulty = string.IsNullOrWhiteSpace(last?.IntensityTag) ? "unknown" : last.IntensityTag;

        float bmi = 0f;
        try
        {
            var roster = _storage.GetTrainerClient(1);
            var client = roster.FirstOrDefault(c => c.Id == _loadedClientId);
            if (client is { Weight: > 0, Height: > 0 })
                bmi = (float)BmiCalculator.Calculate(client.Weight, client.Height);
        }
        catch
        {
            // leave bmi at 0 if profile data is incomplete
        }

        var payload = new NutritionSyncPayload
        {
            TotalCalories = totalCalories,
            WorkoutDifficulty = difficulty,
            UserBmi = bmi
        };

        var ok = await _clientService.SyncNutritionAsync(payload).ConfigureAwait(true);
        if (ok && _nutritionSyncOptions.UseInProcessMock)
        {
            SyncNutritionStatus =
                "Nutrition sync OK (demo mock — no HTTP, flip UseInProcessMock off when their API is up).";
        }
        else if (ok)
        {
            SyncNutritionStatus = "Nutrition sync OK.";
        }
        else
        {
            SyncNutritionStatus =
                "Sync failed — start your local nutrition API (see NutritionSyncOptions default URL) or check the network.";
        }
    }

    public void LoadClientData(int clientId)
    {
        _loadedClientId = clientId;
        var history = _storage.GetWorkoutHistory(clientId);
        var totalCal = history.Sum(h => h.TotalCaloriesBurned);
        CaloriesSummary = $"Calories burned (all logged workouts): {totalCal}";

        var latest = history.FirstOrDefault();
        if (latest != null && latest.Exercises is { Count: > 0 })
        {
            LatestSessionHint = $"Latest session: {latest.WorkoutName} — {latest.Date:g}";
            LoggedExercises = new ObservableCollection<LoggedExercise>(latest.Exercises);
        }
        else
        {
            LatestSessionHint = "No completed workouts with exercises yet.";
            LoggedExercises = new ObservableCollection<LoggedExercise>();
        }

        var plan = _clientService.GetActiveNutritionPlan(clientId);
        if (plan != null)
        {
            var mealList = _storage.GetMealsForPlan(plan.PlanId);
            Meals = new ObservableCollection<Meal>(mealList);
        }
        else
        {
            Meals = new ObservableCollection<Meal>();
        }
    }
}
