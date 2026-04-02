using System.Text.Json;
using Microsoft.Data.Sqlite;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
        // ── Issue #112 ───────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public int InsertNutritionPlan(NutritionPlan plan)
        {
            
            const string sql = @"
                INSERT INTO NUTRITION_PLAN (start_date, end_date)
                VALUES (@StartDate, @EndDate);
                SELECT last_insert_rowid();";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd  = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@StartDate", plan.StartDate.Date.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("@EndDate",   plan.EndDate.Date.ToString("yyyy-MM-dd"));
            conn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()!);
        }

        // ── Issue #113 ───────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void InsertMeal(Meal meal, int nutritionPlanId)
        {
            const string sql = @"
                INSERT INTO MEAL (nutrition_plan_id, name, ingredients, instructions)
                VALUES (@PlanId, @Name, @Ingredients, @Instructions);";

            string serializedIngredients = JsonSerializer.Serialize(meal.Ingredients);

            using var conn = new SqliteConnection(_connectionString);
            using var cmd  = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PlanId",       nutritionPlanId);
            cmd.Parameters.AddWithValue("@Name",         meal.Name);
            cmd.Parameters.AddWithValue("@Ingredients",  serializedIngredients);
            cmd.Parameters.AddWithValue("@Instructions", meal.Instructions);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        // ── Issue #114 ───────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public void AssignNutritionPlanToClient(int clientId, int nutritionPlanId)
        {
            // INSERT OR IGNORE replaces the T-SQL IF NOT EXISTS pattern.
            const string sql = @"
                INSERT OR IGNORE INTO CLIENT_NUTRITION_PLAN (client_id, nutrition_plan_id)
                VALUES (@ClientId, @PlanId);";

            using var conn = new SqliteConnection(_connectionString);
            using var cmd  = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientId", clientId);
            cmd.Parameters.AddWithValue("@PlanId",   nutritionPlanId);
            conn.Open();
            cmd.ExecuteNonQuery();
        }

        /// <inheritdoc/>
        public void SaveNutritionPlanForClient(NutritionPlan plan, int clientId)
        {
            int planId = InsertNutritionPlan(plan);

            foreach (Meal meal in plan.Meals)
                InsertMeal(meal, planId);

            AssignNutritionPlanToClient(clientId, planId);
        }

        // ── Retrieval ────────────────────────────────────────────────────────────

        /// <inheritdoc/>
        public List<NutritionPlan> GetNutritionPlansForClient(int clientId)
        {
            const string sql = @"
                SELECT np.nutrition_plan_id, np.start_date, np.end_date
                FROM   NUTRITION_PLAN np
                JOIN   CLIENT_NUTRITION_PLAN cnp
                       ON np.nutrition_plan_id = cnp.nutrition_plan_id
                WHERE  cnp.client_id = @ClientId
                ORDER  BY np.start_date;";

            var plans = new List<NutritionPlan>();

            using var conn = new SqliteConnection(_connectionString);
            using var cmd  = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ClientId", clientId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                plans.Add(new NutritionPlan
                {
                    PlanId    = reader.GetInt32(0),
                    StartDate = DateTime.Parse(reader.GetString(1)),
                    EndDate   = DateTime.Parse(reader.GetString(2)),
                });
            }

            foreach (NutritionPlan plan in plans)
                plan.Meals = GetMealsForPlan(plan.PlanId);

            return plans;
        }

        /// <inheritdoc/>
        public List<Meal> GetMealsForPlan(int nutritionPlanId)
        {
            const string sql = @"
                SELECT meal_id, nutrition_plan_id, name, ingredients, instructions
                FROM   MEAL
                WHERE  nutrition_plan_id = @PlanId
                ORDER  BY meal_id;";

            var meals = new List<Meal>();

            using var conn = new SqliteConnection(_connectionString);
            using var cmd  = new SqliteCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PlanId", nutritionPlanId);
            conn.Open();

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string rawIngredients = reader.GetString(3);
                List<string> ingredients = JsonSerializer.Deserialize<List<string>>(rawIngredients)
                                           ?? new List<string>();

                meals.Add(new Meal
                {
                    MealId          = reader.GetInt32(0),
                    NutritionPlanId = reader.GetInt32(1),
                    Name            = reader.GetString(2),
                    Ingredients     = ingredients,
                    Instructions    = reader.GetString(4),
                });
            }

            return meals;
        }
    }
}
