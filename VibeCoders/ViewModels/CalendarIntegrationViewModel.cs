using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public class DaySelectionItem : ObservableObject
    {
        private bool _isSelected;

        public int DayOfWeekIndex { get; }
        public string DayName { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public DaySelectionItem(int dayOfWeekIndex, string dayName, bool initialSelection = false)
        {
            DayOfWeekIndex = dayOfWeekIndex;
            DayName = dayName;
            _isSelected = initialSelection;
        }
    }

    public class CalendarIntegrationViewModel : ObservableObject
    {
        private readonly IDataStorage _dataStorage;
        private readonly ICalendarExportService _calendarExportService;
        private readonly IUserSession _userSession;

        private ObservableCollection<WorkoutTemplate> _availableWorkouts = new();
        private WorkoutTemplate? _selectedWorkout;
        private int _durationWeeks = 4;
        private ObservableCollection<DaySelectionItem> _selectedDays = new();
        private bool _isLoading;
        private string _generatedIcsContent = string.Empty;

        public ObservableCollection<WorkoutTemplate> AvailableWorkouts
        {
            get => _availableWorkouts;
            set => SetProperty(ref _availableWorkouts, value);
        }

        public WorkoutTemplate? SelectedWorkout
        {
            get => _selectedWorkout;
            set => SetProperty(ref _selectedWorkout, value);
        }

        public int DurationWeeks
        {
            get => _durationWeeks;
            set => SetProperty(ref _durationWeeks, value);
        }

        public ObservableCollection<DaySelectionItem> SelectedDays
        {
            get => _selectedDays;
            set => SetProperty(ref _selectedDays, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string GeneratedIcsContent
        {
            get => _generatedIcsContent;
            set => SetProperty(ref _generatedIcsContent, value);
        }

        public CalendarIntegrationViewModel(
            IDataStorage dataStorage,
            ICalendarExportService calendarExportService,
            IUserSession userSession)
        {
            _dataStorage = dataStorage ?? throw new ArgumentNullException(nameof(dataStorage));
            _calendarExportService = calendarExportService ?? throw new ArgumentNullException(nameof(calendarExportService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            InitializeDaySelection();
            _ = LoadAvailableWorkoutsAsync();
        }

        private void InitializeDaySelection()
        {
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var defaultSelections = new[] { false, true, true, true, true, true, false };

            SelectedDays.Clear();
            for (int i = 0; i < 7; i++)
            {
                SelectedDays.Add(new DaySelectionItem(i, dayNames[i], defaultSelections[i]));
            }
        }

        public async Task LoadAvailableWorkoutsAsync()
        {
            try
            {
                IsLoading = true;

                var clientId = (int)_userSession.CurrentUserId;
                var workouts = await Task.Run(() => _dataStorage.GetAvailableWorkouts(clientId));

                AvailableWorkouts.Clear();
                foreach (var workout in workouts)
                {
                    AvailableWorkouts.Add(workout);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading workouts: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        public int[] GetSelectedDaysOfWeek()
        {
            return SelectedDays
                .Where(d => d.IsSelected)
                .Select(d => d.DayOfWeekIndex)
                .ToArray();
        }

        public string? ValidateInput()
        {
            if (SelectedWorkout == null)
                return "Please select a workout from the dropdown.";

            if (DurationWeeks < 1 || DurationWeeks > 52)
                return "Duration must be between 1 and 52 weeks.";

            var selectedDaysArray = GetSelectedDaysOfWeek();
            if (selectedDaysArray.Length == 0)
                return "Please select at least one training day.";

            return null;
        }

        public async Task<string> GenerateCalendarAsync()
        {
            return await Task.Run(() =>
            {
                var validationError = ValidateInput();
                if (validationError != null)
                    throw new InvalidOperationException(validationError);

                if (SelectedWorkout == null)
                    throw new InvalidOperationException("No workout selected.");

                var selectedDaysArray = GetSelectedDaysOfWeek();
                var icsContent = _calendarExportService.GenerateCalendar(
                    SelectedWorkout,
                    DurationWeeks,
                    selectedDaysArray);

                GeneratedIcsContent = icsContent;
                return icsContent;
            });
        }

        public void ToggleDaySelection(int dayOfWeek)
        {
            var dayItem = SelectedDays.FirstOrDefault(d => d.DayOfWeekIndex == dayOfWeek);
            if (dayItem != null)
            {
                dayItem.IsSelected = !dayItem.IsSelected;
            }
        }
    }
}