using VibeCoders.Models;
using User = VibeCoders.Models.User;

namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        // ── Database Initialization ──────────────────────────────────────────
        /// <summary>
        /// Creates all required database tables if they do not exist.
        /// Call once at application startup.
        /// </summary>
        void EnsureSchemaCreated();

        /// <summary>
        /// Seeds the database with predefined workout templates if they don't exist.
        /// Call once at application startup after EnsureSchemaCreated.
        /// </summary>
        void SeedPrebuiltWorkouts();

        // ── User ────────────────────────────────────────────────────────────
        bool SaveUser(User u);
        User LoadUser(string username);

        // ── Client ──────────────────────────────────────────────────────────
        bool SaveClientData(Client c);
        List<Client> GetTrainerClient(int trainerId);

        // ── Workout Template ─────────────────────────────────────────────────
        /// <summary>
        /// Returns all workout templates available to the client:
        /// PREBUILT templates visible to everyone, plus any TRAINER_ASSIGNED
        /// templates assigned specifically to this client.
        /// </summary>
        List<WorkoutTemplate> GetAvailableWorkouts(int clientId);

        // ── Workout Log ─────────────────────────────────────────────────────
        bool SaveWorkoutLog(WorkoutLog log);
        List<WorkoutLog> GetWorkoutHistory(int clientId);
        List<WorkoutLog> GetLastTwoLogsForExercise(int templateExerciseId);

        // ── Template Exercise ────────────────────────────────────────────────
        TemplateExercise GetTemplateExercise(int templateExerciseId);
        bool UpdateTemplateWeight(int templateExerciseId, double newWeight);

        // ── Notifications ────────────────────────────────────────────────────
        bool SaveNotification(Notification notification);
        List<Notification> GetNotifications(int clientId);
    }
}