using VibeCoders.Models;
using User = VibeCoders.Models.User;

namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        /// <summary>
        /// Creates SQL Server tables used by the app when missing (idempotent).
        /// </summary>
        void EnsureSchemaCreated();

        // ── User ────────────────────────────────────────────────────────────
        bool SaveUser(User u);
        User? LoadUser(string username);

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
        TemplateExercise? GetTemplateExercise(int templateExerciseId);
        bool UpdateTemplateWeight(int templateExerciseId, double newWeight);

        // ── Notifications ────────────────────────────────────────────────────
        bool SaveNotification(Notification notification);
        List<Notification> GetNotifications(int clientId);

        // ── Achievements ─────────────────────────────────────────────────────
        /// <summary>
        /// Returns the full achievement catalog for <paramref name="clientId"/>, each row
        /// including whether the client has unlocked it. Locked rows are included so the
        /// rank showcase can keep future goals visible (product requirement).
        /// </summary>
        /// <param name="clientId">Client whose <c>CLIENT_ACHIEVEMENT</c> rows join the catalog.</param>
        List<AchievementShowcaseItem> GetAchievementShowcaseForClient(int clientId);

        /// <summary>
        /// Returns the total number of completed workout sessions logged by
        /// <paramref name="clientId"/> (lifetime count, all time).
        /// </summary>
        int GetWorkoutCount(int clientId);

        /// <summary>Returns the number of distinct calendar days on which the client logged a workout.</summary>
        int GetDistinctWorkoutDayCount(int clientId);

        /// <summary>Returns a single achievement with the client's unlock state, or null if not found.</summary>
        AchievementShowcaseItem? GetAchievementForClient(int achievementId, int clientId);

        /// <summary>Updates the workout log's feedback values (rating and notes).</summary>
        bool UpdateWorkoutLogFeedback(int workoutLogId, double rating, string notes);

        /// <summary>Marks a specific achievement as unlocked for the client. Returns false if already unlocked.</summary>
        bool AwardAchievement(int clientId, int achievementId);

        /// <summary>
        /// Checks every milestone achievement whose <c>threshold_workouts</c> the
        /// client has now reached and marks it as unlocked in <c>CLIENT_ACHIEVEMENT</c>.
        /// Idempotent: already-unlocked rows are not touched.
        /// Call this immediately after persisting a new <see cref="WorkoutLog"/>.
        /// </summary>
        /// <param name="clientId">Client to evaluate milestones for.</param>
        void EvaluateAndUnlockWorkoutMilestones(int clientId);

        // ── Streak / weekly helpers (used by EvaluationEngine checks) ────────

        /// <summary>
        /// Returns the client's longest consecutive-day workout streak (ever).
        /// A streak breaks when a calendar day with no workout separates two logged days.
        /// Used by <see cref="Domain.StreakCheck"/>.
        /// </summary>
        int GetConsecutiveWorkoutDayStreak(int clientId);

        /// <summary>
        /// Returns how many workouts the client completed in the last 7 calendar days
        /// (rolling window: today inclusive, going back 6 days).
        /// Used by <see cref="Domain.WeeklyVolumeCheck"/>.
        /// </summary>
        int GetWorkoutsInLastSevenDays(int clientId);
    }
}