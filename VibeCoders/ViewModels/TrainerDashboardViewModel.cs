using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using VibeCoders.Models;
using VibeCoders.Services;
using Windows.Media.Protection.PlayReady;

namespace VibeCoders.ViewModels
{
    public class TrainerDashboardViewModel : INotifyPropertyChanged
    {
        private readonly TrainerService _trainerService;

        public ObservableCollection<Client> AssignedClients { get; set; } = new ObservableCollection<Client>();
        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; set; } = new ObservableCollection<WorkoutLog>();

        private Client _selectedClient;
        public Client SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (_selectedClient != value)
                {
                    _selectedClient = value;
                    LoadLogsForSelectedClient();
                    OnPropertyChanged();
                    _assignNutritionCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        // --- Issue #119 Properties ---

        private DateTimeOffset _nutritionStartDate = DateTimeOffset.Now;
        public DateTimeOffset NutritionStartDate
        {
            get => _nutritionStartDate;
            set
            {
                if (_nutritionStartDate != value)
                {
                    _nutritionStartDate = value;
                    OnPropertyChanged();
                    _assignNutritionCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private DateTimeOffset _nutritionEndDate = DateTimeOffset.Now.AddDays(30);
        public DateTimeOffset NutritionEndDate
        {
            get => _nutritionEndDate;
            set
            {
                if (_nutritionEndDate != value)
                {
                    _nutritionEndDate = value;
                    OnPropertyChanged();
                    _assignNutritionCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        private TrainerRelayCommand _assignNutritionCommand;
        public ICommand AssignNutritionCommand => _assignNutritionCommand;

        // -----------------------------

        public TrainerDashboardViewModel(TrainerService trainerService)
        {
            _trainerService = trainerService;

            _assignNutritionCommand = new TrainerRelayCommand(ExecuteAssignNutrition, CanExecuteAssignNutrition);

            LoadClientsAndWorkouts();
        }

        private void LoadClientsAndWorkouts()
        {
            AssignedClients.Clear();

            var clients = _trainerService.GetAssignedClients(1);

            if (clients != null)
            {
                foreach (var client in clients)
                {
                    AssignedClients.Add(client);
                }
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

        // --- Issue #119 Command Logic ---

        private bool CanExecuteAssignNutrition(object parameter)
        {
            return SelectedClient != null && NutritionEndDate >= NutritionStartDate;
        }

        private void ExecuteAssignNutrition(object parameter)
        {
            if (SelectedClient == null) return;

            var newPlan = new NutritionPlan
            {
                startDate = NutritionStartDate.ToString("yyyy-MM-dd"),
                endDate = NutritionEndDate.ToString("yyyy-MM-dd"),
                meals = new List<Meal>()
            };

            SelectedClient.nutritionPlan = newPlan;

            // Notify UI of the update
            OnPropertyChanged(nameof(SelectedClient));
        }

        // --- INotifyPropertyChanged Implementation ---

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // --- Command Helper ---
    public class TrainerRelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public TrainerRelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}