using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text.Json;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public partial class SqlDataStorage
    {
    
        public int InsertNutritionPlan(NutritionPlan plan)
        {
            int newPlanId = 0;
            string query = @"
                INSERT INTO NUTRITION_PLAN (start_date, end_date) 
                OUTPUT INSERTED.nutrition_plan_id 
                VALUES (@StartDate, @EndDate);";

           
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@StartDate", DateTime.Parse(plan.startDate));
                cmd.Parameters.AddWithValue("@EndDate", DateTime.Parse(plan.endDate));

                conn.Open();
                newPlanId = (int)cmd.ExecuteScalar();
            }

            return newPlanId;
        }

     
        public void InsertMeal(Meal meal, int nutritionPlanId)
        {
            string query = @"
                INSERT INTO MEAL (nutrition_plan_id, name, ingredients, instructions)
                VALUES (@PlanId, @Name, @Ingredients, @Instructions);";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                string serializedIngredients = JsonSerializer.Serialize(meal.ingredients);

                cmd.Parameters.AddWithValue("@PlanId", nutritionPlanId);
                cmd.Parameters.AddWithValue("@Name", meal.name);
                cmd.Parameters.AddWithValue("@Ingredients", serializedIngredients);
                cmd.Parameters.AddWithValue("@Instructions", meal.instructions);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

     
        public void AssignNutritionPlanToClient(int clientId, int nutritionPlanId)
        {
            string query = @"
                INSERT INTO CLIENT_NUTRITION_PLAN (client_id, nutrition_plan_id)
                VALUES (@ClientId, @PlanId);";

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@ClientId", clientId);
                cmd.Parameters.AddWithValue("@PlanId", nutritionPlanId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

       
        public void SaveNutritionPlanForClient(NutritionPlan np, int clientId)
        {
            int generatedPlanId = InsertNutritionPlan(np);

            if (np.meals != null)
            {
                foreach (Meal meal in np.meals)
                {
                    InsertMeal(meal, generatedPlanId);
                }
            }

            AssignNutritionPlanToClient(clientId, generatedPlanId);
        }
    }
}