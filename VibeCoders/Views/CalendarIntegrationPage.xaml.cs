using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;
using Windows.Storage.Pickers;

namespace VibeCoders.Views
{
    public sealed partial class CalendarIntegrationPage : Page
    {
        private CalendarIntegrationViewModel? _viewModel;

        public CalendarIntegrationPage()
        {
            this.InitializeComponent();
            
            // Get ViewModel from DI container
            _viewModel = App.GetService<CalendarIntegrationViewModel>();
            this.DataContext = _viewModel;
            
            // Wire up events when page is loaded.
            this.Loaded += async (s, e) =>
            {
                GenerateCalendarButton.Click += GenerateCalendarButton_Click;

                if (_viewModel != null)
                {
                    await _viewModel.EnsureWorkoutsLoadedAsync();
                }
            };
        }

        private async void GenerateCalendarButton_Click(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null)
                return;

            try
            {
                GenerateCalendarButton.IsEnabled = false;
                
                // Validate input in ViewModel
                string? validationError = _viewModel.ValidateInput();
                if (validationError != null)
                {
                    ShowError(validationError);
                    return;
                }

                // Generate the calendar .ics file asynchronously
                var icsContent = await _viewModel.GenerateCalendarAsync();
                
                if (string.IsNullOrEmpty(icsContent))
                {
                    ShowError("Failed to generate calendar file. Please try again.");
                    return;
                }

                // Prompt user to select save location
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.Downloads;
                savePicker.FileTypeChoices.Add("iCalendar", new System.Collections.Generic.List<string> { ".ics" });
                
                // Get the HWND for the file picker (WinUI 3 requirement)
                var window = (Application.Current as App)?._window;
                if (window == null)
                {
                    ShowError("Unable to access app window for save dialog.");
                    return;
                }

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                if (hWnd == IntPtr.Zero)
                {
                    ShowError("Unable to initialize save dialog window handle.");
                    return;
                }

                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
                
                var file = await savePicker.PickSaveFileAsync();
                
                if (file == null)
                {
                    // User cancelled the save dialog
                    return;
                }

                // Write the .ics content to the file
                await Windows.Storage.FileIO.WriteTextAsync(file, icsContent);
                
                ShowSuccess($"Calendar file '{file.Name}' saved successfully! You can now import it into your calendar application.");
            }
            catch (InvalidOperationException ex)
            {
                // Validation error from ViewModel
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                if (ex is COMException)
                {
                    ShowError("Error saving calendar file: could not open the save dialog.");
                }
                else
                {
                    ShowError($"Error saving calendar file: {ex.Message}");
                }
            }
            finally
            {
                GenerateCalendarButton.IsEnabled = true;
            }
        }

        private void ShowError(string message)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.Title = "Error";
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }

        private void ShowSuccess(string message)
        {
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.Title = "Success";
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }
    }
}

