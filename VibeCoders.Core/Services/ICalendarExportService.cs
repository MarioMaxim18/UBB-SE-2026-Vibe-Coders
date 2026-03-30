using VibeCoders.Models;

namespace VibeCoders.Services
{
    /// <summary>
    /// Service for generating RFC 5545 compliant iCalendar (.ics) files from workout templates.
    /// Handles date calculation, multi-week expansion, and event generation.
    /// </summary>
    public interface ICalendarExportService
    {
        /// <summary>
        /// Generates an .ics file content string for a workout template scheduled across multiple weeks.
        /// </summary>
        /// <param name="workoutTemplate">The workout template to schedule.</param>
        /// <param name="durationWeeks">Number of weeks to expand (1-52).</param>
        /// <param name="selectedDays">Selected days of the week (0 = Sunday, 1 = Monday, ..., 6 = Saturday).</param>
        /// <param name="startDate">Start date for the calendar (defaults to today if null).</param>
        /// <returns>RFC 5545 compliant .ics file content as string.</returns>
        string GenerateCalendar(WorkoutTemplate workoutTemplate, int durationWeeks, int[] selectedDays, DateTime? startDate = null);
    }
}
