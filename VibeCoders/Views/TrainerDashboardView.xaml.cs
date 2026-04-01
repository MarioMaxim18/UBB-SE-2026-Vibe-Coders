using Microsoft.UI.Xaml.Controls;
using System;
using VibeCoders.Services;
using VibeCoders.ViewModels;
using System.Collections.Generic;

namespace VibeCoders.Views
{
    public sealed partial class TrainerDashboardView : Page
    {
        public TrainerDashboardViewModel ViewModel { get; }

        public static string FormatWorkoutDate(DateTime Date)
        {

            return Date.ToString("MMM dd, yyyy");
        }

        public static string FormatLastWorkoutDate(List<VibeCoders.Models.WorkoutLog> logs)
        {
            
            if (logs != null && logs.Count > 0)
            {
                return $"Last Workout: {logs[0].Date.ToString("MMM dd, yyyy")}";
            }

            return "Last Workout: N/A";
        }

        public TrainerDashboardView()
        {
            var service = App.GetService<TrainerService>();
            this.ViewModel = new TrainerDashboardViewModel(service);
            this.InitializeComponent();
        }
    }
}