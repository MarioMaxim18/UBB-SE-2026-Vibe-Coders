using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using VibeCoders.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VibeCoders.ViewModels
{
    public class TrainerDashboardViewModel : INotifyPropertyChanged
    {
        private readonly TrainerService _trainerService;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<Client> AssignedClients { get; set; } = new ObservableCollection<Client>();
        public ObservableCollection<WorkoutLog> SelectedClientLogs { get; set; } = new ObservableCollection<WorkoutLog>();
        public ObservableCollection<ExerciseDisplayRow> CurrentWorkoutDetails { get; set; } = new();
        public ObservableCollection<TemplateExercise> BuilderExercises { get; set; } = new();

        private string _newRoutineName = string.Empty;
        public string NewRoutineName
        {
            get => _newRoutineName;
            set { _newRoutineName = value; OnPropertyChanged(); }
        }

        private Client? _selectedClient;
        public Client? SelectedClient
        {
            get => _selectedClient;
            set
            {
                if (_selectedClient != value)
                {
                    _selectedClient = value;
                    LoadLogsForSelectedClient();
                    OnPropertyChanged();
                }
            }
        }

        

        private WorkoutLog? _selectedWorkoutLog;
        public WorkoutLog? SelectedWorkoutLog
        {
            get => _selectedWorkoutLog;
            set
            {
                if (_selectedWorkoutLog != value)
                {
                    _selectedWorkoutLog = value;
                    OnWorkoutLogSelected();
                    OnPropertyChanged();
                }
            }
        }

        private void OnWorkoutLogSelected()
        {
            CurrentWorkoutDetails.Clear();

            if (_selectedWorkoutLog == null) return;

            foreach (var exercise in _selectedWorkoutLog.Exercises)
            {
                CurrentWorkoutDetails.Add(new ExerciseDisplayRow
                {
                    Name = exercise.ExerciseName,
                    MuscleGroup = exercise.TargetMuscles.ToString(),
                    Sets = exercise.Sets
                });
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
            CurrentWorkoutDetails.Clear();
            if (_selectedClient != null && _selectedClient.WorkoutLog != null)
            {
                var realLogs = _trainerService.GetClientWorkoutHistory(_selectedClient.Id);
                foreach (var log in realLogs)
                {
                    SelectedClientLogs.Add(log);
                }

                if (SelectedClientLogs.Count > 0)
                {
                    SelectedWorkoutLog = SelectedClientLogs[0];
                }

            }
        }

        public void SaveCurrentFeedback()
        {
            if (SelectedWorkoutLog == null) return;

            bool success = _trainerService.SaveWorkoutFeedback(SelectedWorkoutLog);

            if (success)
            {
                System.Diagnostics.Debug.WriteLine("Feedback saved successfully!");

            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Uh oh, feedback failed to save.");
            }
        }
    }
}