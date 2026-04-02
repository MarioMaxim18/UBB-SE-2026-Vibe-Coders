using Microsoft.Data.Sqlite;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage : IDataStorage
    {

        private readonly string _connectionString = DatabasePaths.GetConnectionString();

        /// <summary>
        /// Creates all tables required by the workout tracking and progression
        /// module if they do not already exist. Call this once at application
        /// startup before any other storage operation.
        /// </summary>
        public void EnsureSchemaCreated()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = new SqliteCommand();
            cmd.Connection = conn;

            // Enable foreign keys (SQLite requires explicit activation)
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            cmd.ExecuteNonQuery();

            // ── USER ─────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS [USER] (
                    id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    username      TEXT NOT NULL,
                    password_hash TEXT NOT NULL DEFAULT '',
                    role          TEXT NOT NULL DEFAULT 'CLIENT'
                );";
            cmd.ExecuteNonQuery();

            // ── TRAINER ─────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TRAINER (
                    trainer_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id    INTEGER NOT NULL,
                    FOREIGN KEY (user_id) REFERENCES [USER](id)
                );";
            cmd.ExecuteNonQuery();

            // ── CLIENT ─────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CLIENT (
                    client_id  INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id    INTEGER NOT NULL,
                    trainer_id INTEGER NOT NULL,
                    weight     REAL,
                    height     REAL,
                    FOREIGN KEY (user_id)    REFERENCES [USER](id),
                    FOREIGN KEY (trainer_id) REFERENCES TRAINER(trainer_id)
                );";
            cmd.ExecuteNonQuery();

            // ── EXERCISE LIBRARY ─────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS EXERCISE (
                    exercise_id  INTEGER PRIMARY KEY AUTOINCREMENT,
                    name         TEXT NOT NULL UNIQUE,
                    muscle_group TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();

            // Seed exercises only if table was just created (empty)
            cmd.CommandText = @"
                INSERT OR IGNORE INTO EXERCISE (name, muscle_group) VALUES
                    ('Bench Press',           'CHEST'),
                    ('Incline Dumbbell Press','CHEST'),
                    ('Barbell Squat',         'LEGS'),
                    ('Leg Press',             'LEGS'),
                    ('Deadlift',              'BACK'),
                    ('Pull-Ups',              'BACK'),
                    ('Overhead Press',        'SHOULDERS'),
                    ('Side Laterals',         'SHOULDERS'),
                    ('Bicep Curls',           'ARMS'),
                    ('Tricep Pushdowns',      'ARMS');";
            cmd.ExecuteNonQuery();

            // ── WORKOUT_TEMPLATE ─────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS WORKOUT_TEMPLATE (
                    workout_template_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    client_id           INTEGER NOT NULL,
                    name                TEXT NOT NULL,
                    type                TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();

            // ── TEMPLATE_EXERCISE ────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TEMPLATE_EXERCISE (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    workout_template_id INTEGER NOT NULL,
                    name                TEXT NOT NULL,
                    muscle_group        TEXT NOT NULL,
                    target_sets         INTEGER NOT NULL DEFAULT 3,
                    target_reps         INTEGER NOT NULL DEFAULT 10,
                    target_weight       REAL NOT NULL DEFAULT 0,
                    FOREIGN KEY (workout_template_id) REFERENCES WORKOUT_TEMPLATE(workout_template_id)
                );";
            cmd.ExecuteNonQuery();

            // ── WORKOUT_LOG ──────────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS WORKOUT_LOG (
                    workout_log_id  INTEGER PRIMARY KEY AUTOINCREMENT,
                    client_id       INTEGER NOT NULL,
                    workout_id      INTEGER,
                    date            TEXT NOT NULL,
                    total_duration  TEXT,
                    calories_burned INTEGER,
                    rating          INTEGER,
                    trainer_notes   TEXT,
                    intensity_tag   TEXT NOT NULL DEFAULT '',
                    FOREIGN KEY (client_id)  REFERENCES CLIENT(client_id),
                    FOREIGN KEY (workout_id) REFERENCES WORKOUT_TEMPLATE(workout_template_id)
                );";
            cmd.ExecuteNonQuery();

            // ── WORKOUT_LOG_SETS ─────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS WORKOUT_LOG_SETS (
                    workout_log_sets_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    workout_log_id      INTEGER NOT NULL,
                    exercise_name       TEXT NOT NULL,
                    sets                INTEGER NOT NULL,
                    reps                INTEGER,
                    weight              REAL,
                    target_reps         INTEGER,
                    target_weight       REAL,
                    performance_ratio   REAL,
                    is_system_adjusted  INTEGER NOT NULL DEFAULT 0,
                    adjustment_note     TEXT,
                    FOREIGN KEY (workout_log_id) REFERENCES WORKOUT_LOG(workout_log_id)
                );";
            cmd.ExecuteNonQuery();

            // ── NOTIFICATION ─────────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS NOTIFICATION (
                    id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    client_id    INTEGER NOT NULL,
                    title        TEXT NOT NULL,
                    message      TEXT NOT NULL,
                    type         TEXT NOT NULL,
                    related_id   INTEGER NOT NULL,
                    date_created TEXT NOT NULL,
                    is_read      INTEGER NOT NULL DEFAULT 0,
                    FOREIGN KEY (client_id) REFERENCES CLIENT(client_id)
                );";
            cmd.ExecuteNonQuery();

            // ── ACHIEVEMENT ───────────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS ACHIEVEMENT (
                    achievement_id      INTEGER PRIMARY KEY AUTOINCREMENT,
                    title               TEXT NOT NULL,
                    description         TEXT NOT NULL,
                    criteria            TEXT NOT NULL DEFAULT '',
                    threshold_workouts  INTEGER
                );";
            cmd.ExecuteNonQuery();

            // ── CLIENT_ACHIEVEMENT ────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CLIENT_ACHIEVEMENT (
                    client_id       INTEGER NOT NULL,
                    achievement_id  INTEGER NOT NULL,
                    unlocked        INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (client_id, achievement_id),
                    FOREIGN KEY (client_id)      REFERENCES CLIENT(client_id),
                    FOREIGN KEY (achievement_id) REFERENCES ACHIEVEMENT(achievement_id)
                );";
            cmd.ExecuteNonQuery();

            // ── NUTRITION_PLAN ───────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS NUTRITION_PLAN (
                    nutrition_plan_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    start_date        TEXT NOT NULL,
                    end_date          TEXT NOT NULL
                );";
            cmd.ExecuteNonQuery();

            // ── MEAL ─────────────────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MEAL (
                    meal_id           INTEGER PRIMARY KEY AUTOINCREMENT,
                    nutrition_plan_id INTEGER NOT NULL,
                    name              TEXT NOT NULL,
                    ingredients       TEXT NOT NULL,
                    instructions      TEXT NOT NULL,
                    FOREIGN KEY (nutrition_plan_id) REFERENCES NUTRITION_PLAN(nutrition_plan_id)
                );";
            cmd.ExecuteNonQuery();

            // ── CLIENT_NUTRITION_PLAN ────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS CLIENT_NUTRITION_PLAN (
                    client_id         INTEGER NOT NULL,
                    nutrition_plan_id INTEGER NOT NULL,
                    PRIMARY KEY (client_id, nutrition_plan_id),
                    FOREIGN KEY (client_id)         REFERENCES CLIENT(client_id),
                    FOREIGN KEY (nutrition_plan_id) REFERENCES NUTRITION_PLAN(nutrition_plan_id)
                );";
            cmd.ExecuteNonQuery();

            // ── analytics_workout_log ─────────────────────────────────────────
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS analytics_workout_log (
                    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
                    user_id             INTEGER NOT NULL,
                    workout_name        TEXT NOT NULL,
                    log_date            TEXT NOT NULL,
                    duration_seconds    INTEGER NOT NULL DEFAULT 0,
                    source_template_id  INTEGER NOT NULL DEFAULT 0,
                    total_calories_burned INTEGER NOT NULL DEFAULT 0,
                    intensity_tag       TEXT NOT NULL DEFAULT ''
                );";
            cmd.ExecuteNonQuery();

            // ── Indexes ──────────────────────────────────────────────────────
            cmd.CommandText = @"
                CREATE INDEX IF NOT EXISTS ix_workout_log_client_date
                    ON WORKOUT_LOG (client_id, date DESC, workout_log_id DESC);";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
                CREATE INDEX IF NOT EXISTS ix_workout_log_sets_log_idx
                    ON WORKOUT_LOG_SETS (workout_log_id, sets);";
            cmd.ExecuteNonQuery();
        }
    }
}