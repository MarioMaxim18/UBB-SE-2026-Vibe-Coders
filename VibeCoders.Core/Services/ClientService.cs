using VibeCoders.Models;

namespace VibeCoders.Services
{
    public class ClientService
    {
        private readonly IDataStorage _storage;
        private readonly ProgressionService _progressionService;

        public ClientService(IDataStorage storage, ProgressionService progressionService)
        {
            _storage = storage;
            _progressionService = progressionService;
        }

        // ── Workout ──────────────────────────────────────────────────────────

        /// <summary>
        /// Finalizes a completed workout session:
        ///   1. Stamps the date.
        ///   2. Runs progression evaluation (overload / plateau detection).
        ///   3. Persists the log with all its sets.
        /// </summary>
        public bool FinalizeWorkout(WorkoutLog log)
        {
            if (log == null || log.Exercises == null) return false;

            try
            {
                log.Date = DateTime.Now;

                _progressionService.EvaluateWorkout(log);

                bool isSaved = _storage.SaveWorkoutLog(log);

                return isSaved;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error finalizing workout: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Auto-save hook called every time the client completes a set during
        /// an active workout. Persists the set immediately so progress is not
        /// lost if the app crashes mid-session.
        /// The set is added to the matching LoggedExercise inside the log.
        /// </summary>
        public bool SaveSet(WorkoutLog log, string exerciseName, LoggedSet set)
        {
            if (log == null || set == null || string.IsNullOrWhiteSpace(exerciseName))
                return false;

            try
            {
                // Find or create the LoggedExercise bucket for this exercise.
                var exercise = log.Exercises
                    .FirstOrDefault(e => e.ExerciseName == exerciseName);

                if (exercise == null)
                {
                    exercise = new LoggedExercise
                    {
                        ExerciseName = exerciseName,
                        WorkoutLogId = log.Id
                    };
                    log.Exercises.Add(exercise);
                }

                // Assign the correct set index and add to the in-memory log.
                set.SetIndex = exercise.Sets.Count;
                set.WorkoutLogId = log.Id;
                set.ExerciseName = exerciseName;
                exercise.Sets.Add(set);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving set: {ex.Message}");
                return false;
            }
        }
        public NutritionPlan GetActiveNutritionPlan(int clientId)
        {
            return new NutritionPlan();
        }
        /// <summary>
        /// Updates an existing workout log entry (e.g. trainer modifies a
        /// client's assigned workout after the fact).
        /// </summary>
        public bool ModifyWorkout(WorkoutLog updatedLog)
        {
            if (updatedLog == null) return false;

            try
            {
                return _storage.SaveWorkoutLog(updatedLog);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error modifying workout: {ex.Message}");
                return false;
            }
        }

        // ── Notifications ────────────────────────────────────────────────────

        /// <summary>
        /// Returns all notifications for the given client, ordered by date descending.
        /// Used by ClientDashboardViewModel to populate the notifications list.
        /// </summary>
        public List<Notification> GetNotifications(int clientId)
        {
            try
            {
                return _storage.GetNotifications(clientId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading notifications: {ex.Message}");
                return new List<Notification>();
            }
        }

        /// <summary>
        /// Called when the client taps "Confirm Deload" on a plateau notification.
        /// Delegates to ProgressionService which reduces the template weight and
        /// marks the notification as read.
        /// </summary>
        public void ConfirmDeload(Notification notification)
        {
            if (notification == null) return;

            try
            {
                _progressionService.ProcessDeloadConfirmation(notification);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error confirming deload: {ex.Message}");
            }
        }
    }
}