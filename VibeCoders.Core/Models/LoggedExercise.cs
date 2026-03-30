namespace VibeCoders.Models;

/// <summary>
/// An exercise recorded inside a <see cref="WorkoutLog"/>.
/// </summary>
public sealed class LoggedExercise
{
    //TODO: ATTRIBUTES NEED UPDATE, OR THE DIAGRAM
    public int Id { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int WorkoutLogId { get; set; }
    public float Met { get; set; }
    public List<LoggedSet> Sets { get; set; } = new List<LoggedSet>();
    public List<string> MuscleGroups { get; set; } = new List<string>();
    public int ExerciseCaloriesBurned { get; set; }
    public double? PerformanceRatio { get; set; }
    public bool IsSystemAdjusted { get; set; }
    public string? AdjustmentNote { get; set; }
}
