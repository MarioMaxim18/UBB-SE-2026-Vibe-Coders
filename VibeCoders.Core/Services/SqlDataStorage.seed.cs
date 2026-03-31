using Microsoft.Data.SqlClient;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
        /// <summary>
        /// Seeds the four built-in workout templates if they have not been
        /// inserted yet. Safe to call at every startup — skips insertion when
        /// a PREBUILT template with the same name already exists.
        /// </summary>
        public void SeedPrebuiltWorkouts()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            SeedTemplate(conn, "HIIT Fat Burner", new[]
            {
                ("Jumping Jacks",    "LEGS",      3, 20, 0.0),
                ("Burpees",          "CORE",      3, 15, 0.0),
                ("Mountain Climbers","CORE",      3, 20, 0.0)
            });

            SeedTemplate(conn, "Full Body Mass", new[]
            {
                ("Back Squat",   "LEGS",      4, 8,  60.0),
                ("Bench Press",  "CHEST",     4, 8,  60.0),
                ("Barbell Rows", "BACK",      4, 8,  50.0)
            });

            SeedTemplate(conn, "Full Body Power", new[]
            {
                ("Deadlift",          "BACK",      4, 5,  100.0),
                ("Overhead Press",    "SHOULDERS", 4, 5,  40.0),
                ("Weighted Pull-Ups", "BACK",      4, 5,  10.0)
            });

            SeedTemplate(conn, "Endurance Circuit", new[]
            {
                ("Push-Ups",           "CHEST", 3, 20, 0.0),
                ("Bodyweight Squats",  "LEGS",  3, 25, 0.0),
                ("Plank",              "CORE",  3, 60, 0.0)
            });
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void SeedTemplate(
            SqlConnection conn,
            string name,
            IEnumerable<(string ExerciseName, string MuscleGroup, int Sets, int Reps, double Weight)> exercises)
        {
            // Check if already seeded.
            const string checkSql = @"
                SELECT COUNT(1)
                FROM WORKOUT_TEMPLATE
                WHERE name = @Name AND type = 'PREBUILT';";

            using (var checkCmd = new SqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@Name", name);
                int count = (int)checkCmd.ExecuteScalar();
                if (count > 0) return;
            }

            // Insert the template header.
            const string insertTemplate = @"
                INSERT INTO WORKOUT_TEMPLATE (client_id, name, type)
                VALUES (0, @Name, 'PREBUILT');
                SELECT SCOPE_IDENTITY();";

            int templateId;
            using (var insertCmd = new SqlCommand(insertTemplate, conn))
            {
                insertCmd.Parameters.AddWithValue("@Name", name);
                templateId = Convert.ToInt32(insertCmd.ExecuteScalar());
            }

            // Insert exercises for this template.
            const string insertExercise = @"
                INSERT INTO TEMPLATE_EXERCISE
                    (workout_template_id, name, muscle_group, target_sets, target_reps, target_weight)
                VALUES
                    (@TemplateId, @Name, @MuscleGroup, @Sets, @Reps, @Weight);";

            foreach (var (exerciseName, muscleGroup, sets, reps, weight) in exercises)
            {
                using var exerciseCmd = new SqlCommand(insertExercise, conn);
                exerciseCmd.Parameters.AddWithValue("@TemplateId", templateId);
                exerciseCmd.Parameters.AddWithValue("@Name", exerciseName);
                exerciseCmd.Parameters.AddWithValue("@MuscleGroup", muscleGroup);
                exerciseCmd.Parameters.AddWithValue("@Sets", sets);
                exerciseCmd.Parameters.AddWithValue("@Reps", reps);
                exerciseCmd.Parameters.AddWithValue("@Weight", weight);
                exerciseCmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Inserts baseline achievement definitions when <c>ACHIEVEMENT</c> is empty.
        /// Safe to call on every startup.
        /// </summary>
        public void SeedAchievementCatalog()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            using (var check = new SqlCommand("SELECT COUNT(1) FROM ACHIEVEMENT;", conn))
            {
                var count = (int)check.ExecuteScalar();
                if (count > 0)
                {
                    return;
                }
            }

            void Insert(string title, string description)
            {
                using var cmd = new SqlCommand(
                    "INSERT INTO ACHIEVEMENT (title, description) VALUES (@Title, @Description);",
                    conn);
                cmd.Parameters.AddWithValue("@Title", title);
                cmd.Parameters.AddWithValue("@Description", description);
                cmd.ExecuteNonQuery();
            }

            Insert("First Steps", "Complete your first workout.");
            Insert("Week Warrior", "Log workouts on five different days.");
            Insert("Dedicated", "Reach 50 hours of total active time.");
        }




        /// <summary>
        /// Seeds dummy users, clients, trainers, and workout logs for testing purposes.
        /// Safe to call on every startup.
        /// </summary>
        public void SeedTestData()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // 1. Check if our test trainer already exists. If yes, bail out.
            using (var check = new SqlCommand("SELECT COUNT(1) FROM [USER] WHERE username = 'TestTrainer';", conn))
            {
                if ((int)check.ExecuteScalar() > 0) return;
            }

            // 2. Insert Trainer User
            int trainerUserId;
            using (var cmd = new SqlCommand("INSERT INTO [USER] (username) VALUES ('TestTrainer'); SELECT SCOPE_IDENTITY();", conn))
            {
                trainerUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 3. Insert Trainer Record
            int trainerId;
            using (var cmd = new SqlCommand("INSERT INTO TRAINER (user_id) VALUES (@UserId); SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", trainerUserId);
                trainerId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 4. Insert Client User
            int clientUserId;
            using (var cmd = new SqlCommand("INSERT INTO [USER] (username) VALUES ('TestClient'); SELECT SCOPE_IDENTITY();", conn))
            {
                clientUserId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 5. Insert Client Record (linked to the Trainer!)
            int clientId;
            using (var cmd = new SqlCommand(@"
                INSERT INTO CLIENT (user_id, trainer_id, weight, height) 
                VALUES (@UserId, @TrainerId, 85.5, 180.0); 
                SELECT SCOPE_IDENTITY();", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", clientUserId);
                cmd.Parameters.AddWithValue("@TrainerId", trainerId);
                clientId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // ─── LOCAL HELPERS FOR CLEAN DATA SEEDING ────────────────────────────

            int CreateLog(DateTime date, string duration, int cals)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO WORKOUT_LOG (client_id, date, total_duration, calories_burned, rating) 
                    VALUES (@ClientId, @Date, @Duration, @Cals, 5);
                    SELECT SCOPE_IDENTITY();", conn);
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@Date", date);
                cmd.Parameters.AddWithValue("@Duration", duration);
                cmd.Parameters.AddWithValue("@Cals", cals);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }

            void AddSet(int logId, string exName, int setIndex, int reps, double weight)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO WORKOUT_LOG_SETS (workout_log_id, exercise_name, sets, reps, weight)
                    VALUES (@LogId, @Name, @SetIdx, @Reps, @Weight);", conn);
                cmd.Parameters.AddWithValue("@LogId", logId);
                cmd.Parameters.AddWithValue("@Name", exName);
                cmd.Parameters.AddWithValue("@SetIdx", setIndex);
                cmd.Parameters.AddWithValue("@Reps", reps);
                cmd.Parameters.AddWithValue("@Weight", weight);
                cmd.ExecuteNonQuery();
            }

            // ─── GENERATE WORKOUT HISTORY ─────────────────────────────────────────

            // WORKOUT 1: Heavy Leg Day (Today)
            int log1 = CreateLog(DateTime.Now, "01:15:00", 450);
            AddSet(log1, "Barbell Squat", 1, 10, 100.0);
            AddSet(log1, "Barbell Squat", 2, 8, 105.0);
            AddSet(log1, "Barbell Squat", 3, 6, 110.0);

            AddSet(log1, "Romanian Deadlift", 1, 12, 80.0);
            AddSet(log1, "Romanian Deadlift", 2, 12, 80.0);
            AddSet(log1, "Romanian Deadlift", 3, 10, 85.0);
            AddSet(log1, "Romanian Deadlift", 4, 8, 90.0); // 4 sets to test dynamic layout!

            AddSet(log1, "Calf Raises", 1, 15, 60.0);
            AddSet(log1, "Calf Raises", 2, 15, 60.0);


            // WORKOUT 2: Push Day (3 days ago)
            int log2 = CreateLog(DateTime.Now.AddDays(-3), "00:55:00", 320);
            AddSet(log2, "Bench Press", 1, 10, 80.0);
            AddSet(log2, "Bench Press", 2, 8, 85.0);
            AddSet(log2, "Bench Press", 3, 8, 85.0);

            AddSet(log2, "Overhead Press", 1, 10, 40.0);
            AddSet(log2, "Overhead Press", 2, 10, 40.0);


            // WORKOUT 3: Pull Day (1 week ago)
            int log3 = CreateLog(DateTime.Now.AddDays(-7), "01:05:00", 400);
            AddSet(log3, "Pull-ups", 1, 12, 0.0); // Bodyweight
            AddSet(log3, "Pull-ups", 2, 10, 0.0);
            AddSet(log3, "Pull-ups", 3, 8, 0.0);

            AddSet(log3, "Barbell Row", 1, 10, 60.0);
            AddSet(log3, "Barbell Row", 2, 10, 60.0);
            AddSet(log3, "Barbell Row", 3, 8, 65.0);
        }


    }
}