using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel; // Im pretty sure im going to
using CommunityToolkit.Mvvm.Input;         // need this two things
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public class TrainerDashboardViewModel
    {
        private readonly TrainerService _trainerService;


        public ObservableCollection<Client> AssignedClients { get; set; } = new ObservableCollection<Client>();
        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; set; } = new ObservableCollection<WorkoutLog>();

        private Client? _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set
            {
                // so if we dont put the set property idk how we can expect the ui to update when a client is selected i mean i think
                //update when a client is selected i mean i think this is just better i might be wrong
                if (SetProperty(ref _selectedClient, value))
                {
                    LoadLogsForSelectedClient();
                }
            }
        }


        public TrainerDashboardViewModel(TrainerService trainerService)
        {
            _trainerService = trainerService;
            LoadClientsAndWorkouts();
        }

        private void LoadClientsAndWorkouts()
        {
            AssignedClients.Clear();

            var clients = _trainerService.GetAssignedClients(1);

            foreach (var client in clients)
            {
                AssignedClients.Add(client);
            }
        }

        public void LoadLogsForSelectedClient()
        {
            SelectedClientLogs.Clear();
            if (_selectedClient != null && _selectedClient.WorkoutLog != null)
            {
                foreach (var log in _selectedClient.WorkoutLog)
                {
                    SelectedClientLogs.Add(log);
                }
            }
        }
        

        // -- Meal Builder Properties --

        [ObservableProperty]
        private string newMealName;

        [ObservableProperty]
        private string currentIngredientInput;

        [ObservableProperty]
        private string newMealInstructions;

        public ObservableCollection<string> NewMealIngredients { get; } = new ObservableCollection<string>();

        // --Meal Builder Commands --

        [RelayCommand]
        private void AddIngredient()
        {
            if (!string.IsNullOrWhiteSpace(CurrentIngredientInput) && !NewMealIngredients.Contains(CurrentIngredientInput.Trim()))
            {
                NewMealIngredients.Add(CurrentIngredientInput.Trim());
                CurrentIngredientInput = string.Empty;
            }
        }

        [RelayCommand]
        private void RemoveIngredient(string ingredient)
        {
            if (ingredient != null && NewMealIngredients.Contains(ingredient))
            {
                NewMealIngredients.Remove(ingredient);
            }
        }

        [RelayCommand]
        private void SaveMeal()
        {
            Meal newMeal = new Meal
            {
                Name = NewMealName,
                Ingredients = new List<string>(NewMealIngredients),
                Instructions = NewMealInstructions
            };

            

            // Clear the form for the next meal
            NewMealName = string.Empty;
            NewMealIngredients.Clear();
            NewMealInstructions = string.Empty;
        }

        // I still havent requested a merge for th nutrition Plan implementation but this is going to be needed so --

        [RelayCommand]
        private void AssignNutrition()
        {
            if (SelectedClient != null)
            {
                
                NutritionPlan plan = new NutritionPlan();
                _trainerService.AssignNutritionPlan(SelectedClient, plan);
            }
        }
    }
}
    


