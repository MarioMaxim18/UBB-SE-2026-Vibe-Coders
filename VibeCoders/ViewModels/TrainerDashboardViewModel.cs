using System;
using System.Collections.ObjectModel;
using VibeCoders.Models;
using VibeCoders.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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
        public ObservableCollection<WorkoutTemplate> AssignedWorkouts { get; set; } = new();

        public int EditingTemplateId { get; set; } = 0;

        public void PrepareForEdit(WorkoutTemplate template)
        {
            EditingTemplateId = template.Id;
            NewRoutineName = template.Name;

            BuilderExercises.Clear();
            foreach (var ex in template.GetExercises())
            {
                BuilderExercises.Add(ex);
            }
        }

        public void LoadAssignedWorkouts()
        {
            AssignedWorkouts.Clear();
            if (SelectedClient == null) return;

            
            var allTemplates = _trainerService.DataStorage.GetAvailableWorkouts(SelectedClient.Id);

            // Filter for only the ones the trainer assigned
            var trainerAssigned = allTemplates.Where(t => t.Type == WorkoutType.TRAINER_ASSIGNED);

            foreach (var template in trainerAssigned)
            {
                AssignedWorkouts.Add(template);
            }
        }


        public bool DeleteRoutine(WorkoutTemplate template)
        {
            if (template == null) return false;

            bool success = _trainerService.DataStorage.DeleteWorkoutTemplate(template.Id);

            if (success)
            {
                AssignedWorkouts.Remove(template);
            }

            return success;
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
                    LoadAssignedWorkouts();
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
            LoadAvailableExercises();
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

            }
        }

        public void SaveCurrentFeedback()
        {
            if (SelectedWorkoutLog == null) return;

            bool success = _trainerService.SaveWorkoutFeedback(SelectedWorkoutLog);

            if (success)
                System.Diagnostics.Debug.WriteLine("Feedback saved successfully!");
            else
                System.Diagnostics.Debug.WriteLine("Uh oh, feedback failed to save.");
        }

        // ── Nutrition Plan Assignment (#119) ─────────────────────────────────

        private DateTimeOffset _planStartDate = DateTimeOffset.Now;
        /// <summary>Bound to the Start Date DatePicker control.</summary>
        public DateTimeOffset PlanStartDate
        {
            get => _planStartDate;
            set
            {
                if (_planStartDate != value)
                {
                    _planStartDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanAssignPlan));
                    OnPropertyChanged(nameof(DateRangeError));
                    OnPropertyChanged(nameof(HasDateRangeError));
                }
            }
        }


        }


        // --- WORKOUT BUILDER PROPERTIES ---

        private string _newRoutineName = string.Empty;
        public string NewRoutineName
        {
            get => _newRoutineName;
            set
            {
                if (_newRoutineName != value)
                {
                    _newRoutineName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TemplateExercise> BuilderExercises { get; set; } = new();

        // Temporary mock data for the dropdown (replace with DB data later!)
        public ObservableCollection<string> AvailableExercises { get; set; } = new();
 

        private string? _selectedNewExercise;
        public string? SelectedNewExercise
        {
            get => _selectedNewExercise;
            set
            {
                if (_selectedNewExercise != value)
                {
                    _selectedNewExercise = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _newExerciseSets = 3;
        public double NewExerciseSets
        {
            get => _newExerciseSets;
            set
            {
                if (_newExerciseSets != value)
                {
                    _newExerciseSets = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _newExerciseReps = 10;
        public double NewExerciseReps
        {
            get => _newExerciseReps;
            set
            {
                if (_newExerciseReps != value)
                {
                    _newExerciseReps = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _newExerciseWeight = 0;
        public double NewExerciseWeight
        {
            get => _newExerciseWeight;
            set
            {
                if (_newExerciseWeight != value)
                {
                    _newExerciseWeight = value;
                    OnPropertyChanged();
                }
            }
        }


        public void AddExerciseToRoutine()
        {
            if (string.IsNullOrWhiteSpace(SelectedNewExercise)) return;

            var newExercise = new TemplateExercise
            {
                Name = SelectedNewExercise,
                MuscleGroup = MuscleGroup.OTHER,
                TargetSets = (int)NewExerciseSets, // Cast back to int for the database
                TargetReps = (int)NewExerciseReps,
                TargetWeight = NewExerciseWeight
            };

            BuilderExercises.Add(newExercise);
            SelectedNewExercise = null; // Reset dropdown
        }

        public void RemoveExerciseFromRoutine(TemplateExercise exercise)
        {
            if (BuilderExercises.Contains(exercise))
            {
                BuilderExercises.Remove(exercise);
            }
        }

        public bool SaveRoutine(WorkoutTemplate template)
        {
            // This passes the fully-built routine down to your Service, 
            
            return _trainerService.SaveTrainerWorkout(template);
        }


        private void LoadAvailableExercises()
        {
            AvailableExercises.Clear();

            // This pulls the Bench Press, Squats, etc., we just seeded!
            var libraryNames = _trainerService.DataStorage.GetAllExerciseNames();

            foreach (var name in libraryNames)
            {
                AvailableExercises.Add(name);
            }
        }

    }
        private DateTimeOffset _planEndDate = DateTimeOffset.Now.AddDays(30);
        /// <summary>Bound to the End Date DatePicker control.</summary>
        public DateTimeOffset PlanEndDate
        {
            get => _planEndDate;
            set
            {
                if (_planEndDate != value)
                {
                    _planEndDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanAssignPlan));
                    OnPropertyChanged(nameof(DateRangeError));
                    OnPropertyChanged(nameof(HasDateRangeError));
                }
            }
        }

        /// <summary>Shown below the date pickers when the range is invalid.</summary>
        public string DateRangeError =>
            _planEndDate <= _planStartDate
                ? "End date must be after start date."
                : string.Empty;

        /// <summary>True when the date range is invalid — drives the error label's visibility.</summary>
        public bool HasDateRangeError => _planEndDate <= _planStartDate;

        /// <summary>True only when a client is selected and the date range is valid.</summary>
        public bool CanAssignPlan =>
            _selectedClient != null && _planEndDate > _planStartDate;

        private string _assignmentStatus = string.Empty;
        /// <summary>Confirmation / error message shown after the Assign button is pressed.</summary>
        public string AssignmentStatus
        {
            get => _assignmentStatus;
            private set { _assignmentStatus = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Builds a <see cref="NutritionPlan"/> from the current form values and
        /// confirms the assignment to the trainer service.
        /// </summary>
        public void AssignNutritionPlan()
        {
            if (!CanAssignPlan) return;

            var plan = new NutritionPlan
            {
                ClientId  = _selectedClient!.Id,
                StartDate = _planStartDate.Date,
                EndDate   = _planEndDate.Date,
            };

            AssignmentStatus =
                $"Plan assigned to {_selectedClient.Username}: " +
                $"{plan.StartDate:MMM d, yyyy} – {plan.EndDate:MMM d, yyyy}";

            System.Diagnostics.Debug.WriteLine(
                $"[TrainerDashboard] Nutrition plan assigned — " +
                $"client {plan.ClientId}, {plan.StartDate:d} → {plan.EndDate:d}");
        }
    }
}


        // --- WORKOUT BUILDER PROPERTIES ---

        private string _newRoutineName = string.Empty;
        public string NewRoutineName
        {
            get => _newRoutineName;
            set
            {
                if (_newRoutineName != value)
                {
                    _newRoutineName = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<TemplateExercise> BuilderExercises { get; set; } = new();

        // Temporary mock data for the dropdown (replace with DB data later!)
        public ObservableCollection<string> AvailableExercises { get; set; } = new();
 

        private string? _selectedNewExercise;
        public string? SelectedNewExercise
        {
            get => _selectedNewExercise;
            set
            {
                if (_selectedNewExercise != value)
                {
                    _selectedNewExercise = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _newExerciseSets = 3;
        public double NewExerciseSets
        {
            get => _newExerciseSets;
            set
            {
                if (_newExerciseSets != value)
                {
                    _newExerciseSets = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _newExerciseReps = 10;
        public double NewExerciseReps
        {
            get => _newExerciseReps;
            set
            {
                if (_newExerciseReps != value)
                {
                    _newExerciseReps = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _newExerciseWeight = 0;
        public double NewExerciseWeight
        {
            get => _newExerciseWeight;
            set
            {
                if (_newExerciseWeight != value)
                {
                    _newExerciseWeight = value;
                    OnPropertyChanged();
                }
            }
        }


        public void AddExerciseToRoutine()
        {
            if (string.IsNullOrWhiteSpace(SelectedNewExercise)) return;

            var newExercise = new TemplateExercise
            {
                Name = SelectedNewExercise,
                MuscleGroup = MuscleGroup.OTHER,
                TargetSets = (int)NewExerciseSets, // Cast back to int for the database
                TargetReps = (int)NewExerciseReps,
                TargetWeight = NewExerciseWeight
            };

            BuilderExercises.Add(newExercise);
            SelectedNewExercise = null; // Reset dropdown
        }

        public void RemoveExerciseFromRoutine(TemplateExercise exercise)
        {
            if (BuilderExercises.Contains(exercise))
            {
                BuilderExercises.Remove(exercise);
            }
        }

        public bool SaveRoutine(WorkoutTemplate template)
        {
            // This passes the fully-built routine down to your Service, 
            
            return _trainerService.SaveTrainerWorkout(template);
        }


        private void LoadAvailableExercises()
        {
            AvailableExercises.Clear();

            // This pulls the Bench Press, Squats, etc., we just seeded!
            var libraryNames = _trainerService.DataStorage.GetAllExerciseNames();

            foreach (var name in libraryNames)
            {
                AvailableExercises.Add(name);
            }
        }

    }
}