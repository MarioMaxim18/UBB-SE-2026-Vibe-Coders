namespace VibeCoders.Models
{
    /// <summary>
    /// Muscle groups used to determine weight increment steps during progression.
    /// </summary>
    public enum MuscleGroup
    {
        CHEST,
        BACK,
        LEGS,
        SHOULDERS,
        ARMS,
        CORE,
        OTHER // Add this to handle nulls
    }

    /// <summary>
    /// Describes the origin / ownership of a workout template.
    /// </summary>
    public enum WorkoutType
    {
        CUSTOM,
        PREBUILT,
        TRAINER_ASSIGNED
    }
}