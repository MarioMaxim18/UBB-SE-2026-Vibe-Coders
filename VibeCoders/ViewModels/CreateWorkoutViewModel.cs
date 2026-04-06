using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public class CreateWorkoutViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<MuscleGroup> MuscleGroups { get; } = new(Enum.GetValues<MuscleGroup>());

        private string _workoutName = string.Empty;
        public string WorkoutName
        {
            get => _workoutName;
            set
            {
                _workoutName = value;
                OnPropertyChanged(nameof(WorkoutName));
            }
        }

        private bool _isTrainerCreating;
        public bool IsTrainerCreating
        {
            get => _isTrainerCreating;
            set
            {
                _isTrainerCreating = value;
                OnPropertyChanged(nameof(IsTrainerCreating));
            }
        }

        public ObservableCollection<TemplateExercise> Exercises { get; } = new ObservableCollection<TemplateExercise>();

        // Commands
        public ICommand AddExerciseCommand { get; }
        public ICommand RemoveExerciseCommand { get; }
        public ICommand SaveWorkoutCommand { get; }

        private readonly IDataStorage _dataStorage;

        public int ClientId { get; set; }

        public event Action? WorkoutSaved;

        public CreateWorkoutViewModel(IDataStorage dataStorage)
        {
            _dataStorage = dataStorage;

            AddExerciseCommand = new RelayCommand(AddExercise);
            RemoveExerciseCommand = new RelayCommand<TemplateExercise>(RemoveExercise);
            SaveWorkoutCommand = new RelayCommand(SaveWorkout);
        }

        private void AddExercise()
        {
            Exercises.Add(new TemplateExercise
            {
                Name = "New Exercise",
                TargetSets = 3,
                TargetReps = 10,
                MuscleGroup = MuscleGroup.OTHER
            });
        }

        private void RemoveExercise(TemplateExercise? exercise)
        {
            if (exercise == null)
                return;

            Exercises.Remove(exercise);
        }

        private void SaveWorkout()
        {
            if (string.IsNullOrWhiteSpace(WorkoutName) || Exercises.Count == 0) return;

            var newWorkout = new WorkoutTemplate
            {
                Name = WorkoutName,
                Type = IsTrainerCreating ? WorkoutType.TRAINER_ASSIGNED : WorkoutType.CUSTOM,
                ClientId = ClientId
            };

            foreach (var exercise in Exercises)
                newWorkout.AddExercise(exercise);

            _dataStorage.SaveTrainerWorkout(newWorkout);
            WorkoutSaved?.Invoke();
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
