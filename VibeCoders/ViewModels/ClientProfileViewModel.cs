using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels;

public partial class ClientProfileViewModel : ObservableObject
{
    private readonly IDataStorage _storage;
    private readonly ClientService _clientService;

    [ObservableProperty]
    private ObservableCollection<LoggedExercise> loggedExercises = new();

    [ObservableProperty]
    private ObservableCollection<Meal> meals = new();

    [ObservableProperty]
    private string caloriesSummary = "Calories burned (all logged workouts): 0";

    [ObservableProperty]
    private string latestSessionHint = string.Empty;

    public ClientProfileViewModel(IDataStorage storage, ClientService clientService)
    {
        _storage = storage;
        _clientService = clientService;
    }

    public void LoadClientData(int clientId)
    {
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
