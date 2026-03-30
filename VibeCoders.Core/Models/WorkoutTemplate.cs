namespace VibeCoders.Models
{
    /// <summary>
    /// A workout template that a client can start a session from.
    /// Can be PREBUILT (visible to all), CUSTOM (created by the client),
    /// or TRAINER_ASSIGNED (assigned by the client's trainer).
    /// </summary>
    public class WorkoutTemplate
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public WorkoutType Type { get; set; }

        private readonly List<TemplateExercise> _exercises = new();

        public void AddExercise(TemplateExercise exercise)
        {
            if (exercise == null) return;
            _exercises.Add(exercise);
        }

        public void RemoveExercise(TemplateExercise exercise)
        {
            if (exercise == null) return;
            _exercises.Remove(exercise);
        }

        public List<TemplateExercise> GetExercises()
        {
            return _exercises;
        }
    }
}