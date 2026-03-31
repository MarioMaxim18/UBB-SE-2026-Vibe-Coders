using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    /// <summary>
    /// Drives the active workout session screen.
    /// Loads a WorkoutTemplate, tracks sets in real time via ClientService.SaveSet,
    /// and finalizes the session via ClientService.FinalizeWorkout.
    /// </summary>
    public sealed partial class ActiveWorkoutViewModel : ObservableObject
    {
        private readonly ClientService _clientService;
        private readonly IDataStorage _storage;
        private readonly INavigationService _navigation;
        private WorkoutLog _activeLog;

        public ActiveWorkoutViewModel(
            ClientService clientService,
            IDataStorage storage,
            INavigationService navigation)
        {
            _clientService = clientService;
            _storage = storage;
            _navigation = navigation;
            _activeLog = new WorkoutLog
            {
                Date = DateTime.Now
            };
        }

        // ── Template selection ───────────────────────────────────────────────

        [ObservableProperty]
        private ObservableCollection<WorkoutTemplate> availableWorkouts = new();

        [ObservableProperty]
        private WorkoutTemplate? selectedTemplate;

        [ObservableProperty]
        private bool isLoadingWorkouts;

        /// <summary>
        /// Loads all available workouts for the given client.
        /// Called when the "Available Workouts for You" button is tapped.
        /// </summary>
        [RelayCommand]
        private void LoadAvailableWorkouts(int clientId)
        {
            try
            {
                IsLoadingWorkouts = true;
                AvailableWorkouts.Clear();

                var workouts = _storage.GetAvailableWorkouts(clientId);
                foreach (var w in workouts)
                {
                    AvailableWorkouts.Add(w);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workouts: {ex.Message}");
            }
            finally
            {
                IsLoadingWorkouts = false;
            }
        }

        /// <summary>
        /// Called when the client selects a template from the dropdown.
        /// Initializes the active log and populates the exercise rows.
        /// </summary>
        partial void OnSelectedTemplateChanged(WorkoutTemplate? value)
        {
            if (value == null) return;

            _activeLog = new WorkoutLog
            {
                WorkoutName = value.Name,
                SourceTemplateId = value.Id,
                Date = DateTime.Now
            };

            ExerciseRows.Clear();
            foreach (var exercise in value.GetExercises())
            {
                ExerciseRows.Add(new ActiveExerciseViewModel(exercise));
            }

            IsWorkoutStarted = true;
        }

        // ── Active session ───────────────────────────────────────────────────

        [ObservableProperty]
        private ObservableCollection<ActiveExerciseViewModel> exerciseRows = new();

        [ObservableProperty]
        private bool isWorkoutStarted;

        [ObservableProperty]
        private bool isFinishing;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        /// <summary>
        /// Auto-save hook — called every time the client completes a set.
        /// Delegates to ClientService.SaveSet which tracks the set in-memory.
        /// </summary>
        [RelayCommand]
        private void SaveSet(ActiveSetViewModel setViewModel)
        {
            if (setViewModel == null || SelectedTemplate == null) return;

            var set = new LoggedSet
            {
                ExerciseName = setViewModel.ExerciseName,
                SetIndex = setViewModel.SetIndex,
                ActualReps = setViewModel.ActualReps,
                ActualWeight = setViewModel.ActualWeight,
                TargetReps = setViewModel.TargetReps,
                TargetWeight = setViewModel.TargetWeight
            };

            _clientService.SaveSet(_activeLog, setViewModel.ExerciseName, set);
            setViewModel.IsCompleted = true;

            // Move focus to next set.
            FocusNextSet(setViewModel);
        }

        /// <summary>
        /// Finalizes the workout: runs progression evaluation and persists to DB.
        /// </summary>
        [RelayCommand]
        private void FinishWorkout(int clientId)
        {
            if (!IsWorkoutStarted) return;

            try
            {
                IsFinishing = true;
                ErrorMessage = string.Empty;

                _activeLog.ClientId = clientId;
                _activeLog.Duration = DateTime.Now - _activeLog.Date;

                bool success = _clientService.FinalizeWorkout(_activeLog);

                if (success)
                {
                    LastCompletedLog = _activeLog;
                    IsWorkoutStarted = false;
                    ExerciseRows.Clear();
                    _activeLog = new WorkoutLog { Date = DateTime.Now };

                    // Navigate back to dashboard and trigger a refresh (#66).
                    _navigation.NavigateToClientDashboard(requestRefresh: true);
                }
                else
                {
                    ErrorMessage = "Failed to save workout. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error finishing workout: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                IsFinishing = false;
            }
        }

        /// <summary>
        /// Stores the last completed workout log so it can be repeated.
        /// Set by FinishWorkout after a successful save.
        /// </summary>
        public WorkoutLog? LastCompletedLog { get; private set; }

        /// <summary>
        /// Repeats the last completed workout by reloading the same template
        /// and resetting all sets to their target values. (#77)
        /// </summary>
        [RelayCommand]
        private void RepeatWorkout(int clientId)
        {
            if (LastCompletedLog == null) return;

            var template = _storage.GetAvailableWorkouts(clientId)
                .FirstOrDefault(t => t.Id == LastCompletedLog.SourceTemplateId);

            if (template == null) return;

            SelectedTemplate = template;
        }

        [ObservableProperty]
        private ObservableCollection<Notification> notifications = new();

        [RelayCommand]
        private void LoadNotifications(int clientId)
        {
            Notifications.Clear();
            var list = _clientService.GetNotifications(clientId);
            foreach (var n in list)
            {
                Notifications.Add(n);
            }
        }

        [RelayCommand]
        private void ConfirmDeload(Notification notification)
        {
            if (notification == null) return;
            _clientService.ConfirmDeload(notification);
            Notifications.Remove(notification);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Moves focus to the next incomplete set after one is completed.
        /// Task #67 — Next Set Focus Navigation.
        /// </summary>
        private void FocusNextSet(ActiveSetViewModel completedSet)
        {
            foreach (var exercise in ExerciseRows)
            {
                foreach (var set in exercise.Sets)
                {
                    if (!set.IsCompleted)
                    {
                        set.IsFocused = true;
                        return;
                    }
                }
            }
        }
    }

    // ── Supporting ViewModels ────────────────────────────────────────────────

    /// <summary>
    /// Represents one exercise row in the active workout screen.
    /// </summary>
    public sealed partial class ActiveExerciseViewModel : ObservableObject
    {
        public string ExerciseName { get; }
        public MuscleGroup MuscleGroup { get; }
        public ObservableCollection<ActiveSetViewModel> Sets { get; } = new();

        [ObservableProperty]
        private bool isSystemAdjusted;

        [ObservableProperty]
        private string adjustmentNote = string.Empty;

        public ActiveExerciseViewModel(TemplateExercise template)
        {
            ExerciseName = template.Name;
            MuscleGroup = template.MuscleGroup;

            for (int i = 0; i < template.TargetSets; i++)
            {
                Sets.Add(new ActiveSetViewModel
                {
                    ExerciseName = template.Name,
                    SetIndex = i,
                    TargetReps = template.TargetReps,
                    TargetWeight = template.TargetWeight,
                    IsFocused = i == 0  // first set starts focused
                });
            }
        }
    }

    /// <summary>
    /// Represents one set row in the active workout screen.
    /// Task #72 — Active Workout Set Row Component.
    /// </summary>
    public sealed partial class ActiveSetViewModel : ObservableObject
    {
        public string ExerciseName { get; set; } = string.Empty;
        public int SetIndex { get; set; }
        public int? TargetReps { get; set; }
        public double? TargetWeight { get; set; }

        [ObservableProperty]
        private int? actualReps;

        [ObservableProperty]
        private double? actualWeight;

        [ObservableProperty]
        private bool isCompleted;

        [ObservableProperty]
        private bool isFocused;
    }
}