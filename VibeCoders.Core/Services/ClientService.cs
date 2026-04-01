using System.Net.Http;
using System.Net.Http.Json;
using VibeCoders.Models;
using VibeCoders.Models.Integration;

namespace VibeCoders.Services
{
    public class ClientService
    {
        private readonly IDataStorage _storage;
        private readonly ProgressionService _progressionService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EvaluationEngine _evaluationEngine;
        private readonly IAchievementUnlockedBus _achievementBus;

        private const string NutritionApiEndpoint = "https://nutrition-app.vibecoders.internal/api/nutrition/sync";

        public ClientService(
            IDataStorage storage,
            ProgressionService progressionService,
            IHttpClientFactory httpClientFactory,
            EvaluationEngine evaluationEngine,
            IAchievementUnlockedBus achievementBus)
        {
            _storage            = storage;
            _progressionService = progressionService;
            _httpClientFactory  = httpClientFactory;
            _evaluationEngine   = evaluationEngine;
            _achievementBus     = achievementBus;
        }

        // ── Workout ──────────────────────────────────────────────────────────

        /// <summary>
        /// Finalizes a completed workout session (#191):
        ///   1. Stamps the current date/time on the log.
        ///   2. Runs progression evaluation (overload / plateau detection).
        ///   3. Persists the log with all its sets.
        ///   4. Triggers the <see cref="EvaluationEngine"/> in the background —
        ///      every registered milestone check runs, newly earned badges are
        ///      awarded exactly once, and each unlock is published to the
        ///      <see cref="IAchievementUnlockedBus"/> so the UI can react.
        /// </summary>
        public bool FinalizeWorkout(WorkoutLog log)
        {
            if (log == null || log.Exercises == null) return false;

            try
            {
                log.Date = DateTime.Now;
                _progressionService.EvaluateWorkout(log);

                bool isSaved = _storage.SaveWorkoutLog(log);
                if (!isSaved) return false;

                // Run all milestone checks and notify the UI for each new badge.
                RunAchievementEvaluation(log.ClientId);

                return true;
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

        // ── Nutrition Sync ───────────────────────────────────────────────────

        /// <summary>
        /// Serializes <paramref name="payload"/> to JSON and POSTs it to the
        /// Nutrition App's sync endpoint. Returns <c>true</c> on HTTP 2xx.
        /// </summary>
        public async Task<bool> SyncNutritionAsync(
            NutritionSyncPayload payload,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client
                    .PostAsJsonAsync(NutritionApiEndpoint, payload, cancellationToken)
                    .ConfigureAwait(false);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error syncing nutrition: {ex.Message}");
                return false;
            }
        }

        // ── Achievement Evaluation ───────────────────────────────────────────

        /// <summary>
        /// Delegates to <see cref="EvaluationEngine.Evaluate"/> which runs every
        /// registered <see cref="Domain.IMilestoneCheck"/> rule. For each badge
        /// newly unlocked, fetches the full <see cref="AchievementShowcaseItem"/>
        /// and publishes it on <see cref="IAchievementUnlockedBus"/> so the UI can
        /// display an unlock toast / popup.
        /// Errors are swallowed so a badge evaluation failure never rolls back a
        /// successfully saved workout.
        /// </summary>
        private void RunAchievementEvaluation(int clientId)
        {
            try
            {
                var newlyUnlocked = _evaluationEngine.Evaluate(clientId);

                foreach (var title in newlyUnlocked)
                {
                    // Reload catalog to get the freshly-awarded item's full data.
                    var catalog = _storage.GetAchievementShowcaseForClient(clientId);
                    var item    = catalog.FirstOrDefault(
                        a => string.Equals(a.Title, title, StringComparison.OrdinalIgnoreCase));

                    if (item != null)
                        _achievementBus.NotifyUnlocked(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[ClientService] Achievement evaluation error for client {clientId}: {ex.Message}");
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
