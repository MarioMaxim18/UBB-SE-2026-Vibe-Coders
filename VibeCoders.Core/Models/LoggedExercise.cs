namespace VibeCoders.Models;

/// <summary>
/// An exercise recorded inside a <see cref="WorkoutLog"/>.
/// Carries both the raw performance data and the progression
/// analysis results written by <see cref="Services.ProgressionService"/>.
/// </summary>
public sealed class LoggedExercise
{
    // ?? Identity ?????????????????????????????????????????????????????????????
    public int Id { get; set; }
    public string ExerciseName { get; set; } = string.Empty;
    public int WorkoutLogId { get; set; }

    /// <summary>
    /// FK to the <see cref="TemplateExercise"/> this log entry was generated from.
    /// 0 means the exercise was added ad-hoc (no template source).
    /// </summary>
    public int ParentTemplateExerciseId { get; set; }

    // ?? Performance ???????????????????????????????????????????????????????????
    public List<LoggedSet> Sets { get; set; } = new();
    public MuscleGroup TargetMuscles { get; set; }
    public float Met { get; set; }
    public int ExerciseCaloriesBurned { get; set; }

    // ?? Progression analysis (written by ProgressionService) ?????????????????
    /// <summary>
    /// Average ratio of actual reps to target reps across all sets.
    /// >= 1.0 means overload possible; below 0.9 twice consecutively means plateau.
    /// </summary>
    public double PerformanceRatio { get; set; }

    /// <summary>
    /// True when the system automatically changed the target weight
    /// for the next session. Drives the "System Adjusted" badge in the UI.
    /// </summary>
    public bool IsSystemAdjusted { get; set; }

    /// <summary>
    /// Human-readable explanation of the adjustment, shown in the tooltip.
    /// </summary>
    public string AdjustmentNote { get; set; } = string.Empty;
}