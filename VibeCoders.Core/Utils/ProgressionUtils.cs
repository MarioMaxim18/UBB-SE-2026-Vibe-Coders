using VibeCoders.Models;

namespace VibeCoders.Utils
{
    /// <summary>
    /// Pure static helpers used by <see cref="Services.ProgressionService"/>.
    /// No side effects, no dependencies — easy to unit test.
    /// </summary>
    public static class ProgressionUtils
    {
        // ── Weight increment constants (kg) ──────────────────────────────────
        // Smaller muscles (ARMS, SHOULDERS, CORE) use a 1.25 kg step.
        // Large compound muscles (CHEST, BACK, LEGS) use a 2.5 kg step.
        private const double LARGE_MUSCLE_INCREMENT = 2.5;
        private const double SMALL_MUSCLE_INCREMENT = 1.25;

        // Deload factor: reduce current weight by 10 % when a plateau is confirmed.
        private const double DELOAD_FACTOR = 0.90;

        /// <summary>
        /// Calculates the performance ratio for a single set.
        /// ratio = actualReps / targetReps
        /// ≥ 1.0 → the client hit or exceeded the target (overload is possible).
        /// &lt; 0.9 → the client struggled (potential plateau signal).
        /// </summary>
        /// <param name="actualReps">Reps the client actually performed.</param>
        /// <param name="targetReps">Reps prescribed by the template.</param>
        /// <returns>
        /// A non-negative ratio. Returns 0 when <paramref name="targetReps"/> is
        /// zero or negative to avoid division-by-zero.
        /// </returns>
        public static double CalculateRatio(int actualReps, int targetReps)
        {
            if (targetReps <= 0) return 0;
            return (double)actualReps / targetReps;
        }

        /// <summary>
        /// Returns the weight increment (in kg) to apply after a successful overload,
        /// based on the primary muscle group of the exercise.
        /// </summary>
        /// <param name="muscleGroup">The <see cref="MuscleGroup"/> of the exercise.</param>
        /// <returns>2.5 kg for large muscle groups, 1.25 kg for smaller ones.</returns>
        public static double DetermineWeightIncrement(MuscleGroup muscleGroup)
        {
            switch (muscleGroup)
            {
                case MuscleGroup.CHEST:
                case MuscleGroup.BACK:
                case MuscleGroup.LEGS:
                    return LARGE_MUSCLE_INCREMENT;

                case MuscleGroup.SHOULDERS:
                case MuscleGroup.ARMS:
                case MuscleGroup.CORE:
                    return SMALL_MUSCLE_INCREMENT;

                default:
                    return SMALL_MUSCLE_INCREMENT;
            }
        }

        /// <summary>
        /// Calculates the deloaded weight after a plateau is confirmed.
        /// Reduces the current weight by <see cref="DELOAD_FACTOR"/> (10 %).
        /// The result is rounded to the nearest 0.25 kg to match standard plate increments.
        /// </summary>
        /// <param name="currentWeight">The current prescribed weight in kg.</param>
        /// <returns>The new deloaded weight, minimum 0.</returns>
        public static double CalculateDeload(double currentWeight)
        {
            double raw = currentWeight * DELOAD_FACTOR;
            // Round to nearest 0.25 kg
            double rounded = Math.Round(raw * 4) / 4.0;
            return Math.Max(0, rounded);
        }
    }
}