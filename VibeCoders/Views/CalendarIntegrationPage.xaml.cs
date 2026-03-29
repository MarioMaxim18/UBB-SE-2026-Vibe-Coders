using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace VibeCoders.Views
{
    public sealed partial class CalendarIntegrationPage : Page
    {
        public CalendarIntegrationPage()
        {
            this.InitializeComponent();
        }

        private void GenerateCalendarButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            // Validate workout selection
            if (WorkoutComboBox.SelectedIndex == -1)
            {
                ShowErrorToast("Please select a workout from the dropdown.");
                return;
            }

            // Validate duration input
            string durationInput = DurationWeeksTextBox.Text.Trim();
            if (string.IsNullOrEmpty(durationInput))
            {
                ShowErrorToast("Please enter the number of weeks (1-52).");
                return;
            }

            if (!int.TryParse(durationInput, out int weeks))
            {
                ShowErrorToast("Duration must be a number between 1 and 52.");
                return;
            }

            if (weeks < 1 || weeks > 52)
            {
                ShowErrorToast("Duration must be between 1 and 52 weeks.");
                return;
            }

            // Validate at least one training day selected
            if (!IsAnyDaySelected())
            {
                ShowErrorToast("Please select at least one training day.");
                return;
            }

            // TODO: Generate calendar with validated data
            string selectedWorkout = ((ComboBoxItem)WorkoutComboBox.SelectedItem).Content.ToString();
            ShowSuccessToast($"Calendar will be generated for {selectedWorkout} - {weeks} weeks");
        }

        private bool IsAnyDaySelected()
        {
            return DayMonday.IsChecked == true ||
                   DayTuesday.IsChecked == true ||
                   DayWednesday.IsChecked == true ||
                   DayThursday.IsChecked == true ||
                   DayFriday.IsChecked == true ||
                   DaySaturday.IsChecked == true ||
                   DaySunday.IsChecked == true;
        }

        private void ShowErrorToast(string message)
        {
            var toast = new TeachingTip()
            {
                Title = "Validation Error",
                Subtitle = message,
                CloseButtonContent = "OK",
                Background = new SolidColorBrush(Microsoft.UI.Colors.ErrorRed)
            };
            toast.IsOpen = true;
        }

        private void ShowSuccessToast(string message)
        {
            var toast = new TeachingTip()
            {
                Title = "Success",
                Subtitle = message,
                CloseButtonContent = "OK",
                Background = new SolidColorBrush(Microsoft.UI.Colors.Success)
            };
            toast.IsOpen = true;
        }
    }
}
