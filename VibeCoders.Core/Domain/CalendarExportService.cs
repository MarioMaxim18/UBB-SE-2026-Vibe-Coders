using System;
using System.Text;
using VibeCoders.Models;

namespace VibeCoders.Domain
{
    /// <summary>
    /// Generates RFC 5545 compliant iCalendar (.ics) files from workout templates.
    /// Handles multi-week expansion, date calculation, and exercise formatting.
    /// </summary>
    public class CalendarExportService : VibeCoders.Services.ICalendarExportService
    {
        /// <summary>
        /// Generates an .ics file content string for a workout template scheduled across multiple weeks.
        /// The returned string is in RFC 5545 format and ready for file export.
        /// </summary>
        public string GenerateCalendar(WorkoutTemplate workoutTemplate, int durationWeeks, int[] selectedDays, DateTime? startDate = null)
        {
            if (workoutTemplate == null)
                throw new ArgumentNullException(nameof(workoutTemplate));
            
            if (durationWeeks < 1 || durationWeeks > 52)
                throw new ArgumentOutOfRangeException(nameof(durationWeeks), "Duration must be between 1 and 52 weeks.");
            
            if (selectedDays == null || selectedDays.Length == 0)
                throw new ArgumentException("At least one day must be selected.", nameof(selectedDays));

            var baseDate = startDate ?? DateTime.Now;
            var icsBuilder = new StringBuilder();
            
            // ICS Header
            icsBuilder.AppendLine("BEGIN:VCALENDAR");
            icsBuilder.AppendLine("VERSION:2.0");
            icsBuilder.AppendLine("PRODID:-//VibeCoders//Fitness//EN");
            icsBuilder.AppendLine("CALSCALE:GREGORIAN");
            icsBuilder.AppendLine("METHOD:PUBLISH");
            
            // Generate events for each week and selected day
            var generatedEvents = GenerateWorkoutEvents(workoutTemplate, durationWeeks, selectedDays, baseDate);
            foreach (var eventContent in generatedEvents)
            {
                icsBuilder.AppendLine(eventContent);
            }
            
            // ICS Footer
            icsBuilder.AppendLine("END:VCALENDAR");
            
            return icsBuilder.ToString();
        }

        /// <summary>
        /// Generates individual VEVENT blocks for each workout session across the specified weeks.
        /// </summary>
        private List<string> GenerateWorkoutEvents(WorkoutTemplate workoutTemplate, int durationWeeks, int[] selectedDays, DateTime baseDate)
        {
            var events = new List<string>();
            var selectedDaysHash = new HashSet<int>(selectedDays);
            
            // Iterate through each week
            for (int week = 0; week < durationWeeks; week++)
            {
                // Iterate through each day of the current week
                for (int dayOffset = 0; dayOffset < 7; dayOffset++)
                {
                    // Calculate day of week (0 = Sunday, 1 = Monday, etc.)
                    var currentDate = baseDate.AddDays(week * 7 + dayOffset);
                    int dayOfWeek = (int)currentDate.DayOfWeek;
                    
                    // Check if this day is selected
                    if (!selectedDaysHash.Contains(dayOfWeek))
                        continue;
                    
                    // Create event for this day
                    var eventContent = CreateVEvent(workoutTemplate, currentDate);
                    events.Add(eventContent);
                }
            }
            
            return events;
        }

        /// <summary>
        /// Creates a single VEVENT block for a workout session on the specified date.
        /// </summary>
        private string CreateVEvent(WorkoutTemplate workoutTemplate, DateTime eventDate)
        {
            var builder = new StringBuilder();
            
            // Event start: 10:00 AM on the specified date
            var eventStart = eventDate.Date.AddHours(10);
            var eventEnd = eventStart.AddHours(1); // 1-hour session
            
            builder.AppendLine("BEGIN:VEVENT");
            builder.AppendLine($"DTSTART:{FormatIcsDateTime(eventStart)}");
            builder.AppendLine($"DTEND:{FormatIcsDateTime(eventEnd)}");
            builder.AppendLine($"SUMMARY:{EscapeIcsText(workoutTemplate.Name)}");
            
            // Build description with exercises
            var exerciseDescription = BuildExerciseDescription(workoutTemplate.GetExercises());
            builder.AppendLine($"DESCRIPTION:{EscapeIcsText(exerciseDescription)}");
            
            // Generate unique ID based on template, date
            string uid = $"{workoutTemplate.Id}-{eventDate:yyyyMMdd}@vibecode.local";
            builder.AppendLine($"UID:{uid}");
            
            // Timestamp
            builder.AppendLine($"DTSTAMP:{FormatIcsDateTime(DateTime.UtcNow)}");
            
            builder.AppendLine("END:VEVENT");
            
            return builder.ToString();
        }

        /// <summary>
        /// Builds a formatted description string from the list of exercises.
        /// Format: "Exercise Name - SetsxReps @ WeightKg"
        /// </summary>
        private string BuildExerciseDescription(List<TemplateExercise> exercises)
        {
            if (exercises == null || exercises.Count == 0)
                return "No exercises specified.";
            
            var lines = new List<string>();
            foreach (var exercise in exercises)
            {
                string line = $"{exercise.Name} - {exercise.TargetSets}x{exercise.TargetReps} @ {exercise.TargetWeight}kg";
                lines.Add(line);
            }
            
            return string.Join("\n", lines);
        }

        /// <summary>
        /// Formats a DateTime into RFC 5545 format (YYYYMMDDTHHMMSSZ for UTC).
        /// </summary>
        private string FormatIcsDateTime(DateTime dateTime)
        {
            // Convert to UTC and format as RFC 5545 requires
            var utcDateTime = dateTime.ToUniversalTime();
            return utcDateTime.ToString("yyyyMMddTHHmmssZ");
        }

        /// <summary>
        /// Escapes special characters in ICS text fields per RFC 5545.
        /// Must escape: backslash, semicolon, comma, and newlines.
        /// </summary>
        private string EscapeIcsText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            
            return text
                .Replace("\\", "\\\\")  // Backslash first
                .Replace(";", "\\;")
                .Replace(",", "\\,")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n");
        }
    }
}
