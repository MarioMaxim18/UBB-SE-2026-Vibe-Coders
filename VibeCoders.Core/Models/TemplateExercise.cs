namespace VibeCoders.Models
{
    /// <summary>
    /// A single exercise slot inside a <see cref="WorkoutTemplate"/>.
    /// Stores the target parameters the progression engine compares against.
    /// </summary>
    public class TemplateExercise
    {
        public int Id { get; set; }

        /// <summary>Display name of the exercise (e.g. "Bench Press").</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Back-reference to the template that owns this exercise.</summary>
        public int WorkoutTemplateId { get; set; }

        /// <summary>
        /// The muscle group this exercise targets.
        /// Used by <see cref="Utils.ProgressionUtils.DetermineWeightIncrement"/> to pick the right increment.
        /// </summary>
        public MuscleGroup MuscleGroup { get; set; }

        /// <summary>Number of sets prescribed by the template.</summary>
        public int TargetSets { get; set; }

        /// <summary>
        /// Rep target per set.
        /// <see cref="Services.ProgressionService"/> compares actual reps against this value.
        /// </summary>
        public int TargetReps { get; set; }

        /// <summary>
        /// Current prescribed weight in kg.
        /// Updated by the progression engine after a successful overload.
        /// </summary>
        public double TargetWeight { get; set; }
    }
}