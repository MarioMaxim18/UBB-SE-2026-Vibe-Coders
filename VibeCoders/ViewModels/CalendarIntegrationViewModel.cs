using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    /// <summary>
    /// Represents the selection state of a single day of the week.
    /// Provides observable properties for UI binding.
    /// </summary>
    public partial class DaySelectionItem : ObservableObject
    {
        public int DayOfWeekIndex { get; }
        public string DayName { get; }

        [ObservableProperty]
        private bool isSelected;

        public DaySelectionItem(int dayOfWeekIndex, string dayName, bool initialSelection = false)
        {
            DayOfWeekIndex = dayOfWeekIndex;
            DayName = dayName;
            IsSelected = initialSelection;
        }
    }

    /// <summary>
    /// ViewModel for the Calendar Integration feature.
    /// Manages workout template selection, duration, day selection, and .ics file generation.
    /// </summary>
    public partial class CalendarIntegrationViewModel : ObservableObject
    {
        private readonly IDataStorage _dataStorage;
        private readonly ICalendarExportService _calendarExportService;
        private readonly IUserSession _userSession;

        [ObservableProperty]
        private ObservableCollection<WorkoutTemplate> availableWorkouts = new();

        [ObservableProperty]
        private WorkoutTemplate? selectedWorkout;

        [ObservableProperty]
        private int durationWeeks = 4; // Default to 4 weeks

        [ObservableProperty]
        private ObservableCollection<DaySelectionItem> selectedDays = new();

        [ObservableProperty]
        private bool isLoading = false;

        [ObservableProperty]
        private string generatedIcsContent = string.Empty;

        public CalendarIntegrationViewModel(IDataStorage dataStorage, ICalendarExportService calendarExportService, IUserSession userSession)
        {
            _dataStorage = dataStorage ?? throw new ArgumentNullException(nameof(dataStorage));
            _calendarExportService = calendarExportService ?? throw new ArgumentNullException(nameof(calendarExportService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            // Initialize day selection items (0=Sunday, 1=Monday, etc.)
            InitializeDaySelection();

            _ = LoadAvailableWorkoutsAsync();
        }

        /// <summary>
        /// Initializes the day selection collection with all 7 days (Sunday through Saturday).
        /// Sets default selection to Monday-Friday.
        /// </summary>
        private void InitializeDaySelection()
        {
            var dayNames = new[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            var defaultSelections = new[] { false, true, true, true, true, true, false }; // Mon-Fri

            selectedDays.Clear();
            for (int i = 0; i < 7; i++)
            {
                selectedDays.Add(new DaySelectionItem(i, dayNames[i], defaultSelections[i]));
            }
        }

        /// <summary>
        /// Loads all available workout templates for the current user from the backend.
        /// </summary>
        public async Task LoadAvailableWorkoutsAsync()
        {
            try
            {
                isLoading = true;
                
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
                isLoading = false;
            }
        }

        /// <summary>
        /// Gets the array of selected day-of-week indices (0=Sunday, 6=Saturday).
        /// </summary>
        public int[] GetSelectedDaysOfWeek()
        {
            return selectedDays
                .Where(d => d.IsSelected)
                .Select(d => d.DayOfWeekIndex)
                .ToArray();
        }

        /// <summary>
        /// Validates user input before generation.
        /// Returns error message if invalid, null if valid.
        /// </summary>
        public string? ValidateInput()
        {
            if (selectedWorkout == null)
                return "Please select a workout from the dropdown.";

            if (durationWeeks < 1 || durationWeeks > 52)
                return "Duration must be between 1 and 52 weeks.";

            var selectedDaysArray = GetSelectedDaysOfWeek();
            if (selectedDaysArray.Length == 0)
                return "Please select at least one training day.";

            return null;
        }

        /// <summary>
        /// Generates the .ics file content based on selected workout, duration, and days.
        /// Returns the generated .ics content, or throws exception on validation failure.
        /// </summary>
        public async Task<string> GenerateCalendarAsync()
        {
            return await Task.Run(() =>
            {
                var validationError = ValidateInput();
                if (validationError != null)
                    throw new InvalidOperationException(validationError);

                if (selectedWorkout == null)
                    throw new InvalidOperationException("No workout selected.");

                var selectedDaysArray = GetSelectedDaysOfWeek();
                var icsContent = _calendarExportService.GenerateCalendar(selectedWorkout, durationWeeks, selectedDaysArray);
                GeneratedIcsContent = icsContent;
                return icsContent;
            });
        }

        /// <summary>
        /// Toggles the selection state of a specific day of the week.
        /// </summary>
        public void ToggleDaySelection(int dayOfWeek)
        {
            var dayItem = selectedDays.FirstOrDefault(d => d.DayOfWeekIndex == dayOfWeek);
            if (dayItem != null)
            {
                dayItem.IsSelected = !dayItem.IsSelected;
            }
        }
    }
}
