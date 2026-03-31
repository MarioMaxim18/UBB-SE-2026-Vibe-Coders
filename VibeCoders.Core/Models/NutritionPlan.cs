using System.Collections.Generic;

namespace VibeCoders.Models
{
    public class NutritionPlan
    {
        public int planId { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public List<Meal> meals { get; set; } = new List<Meal>();
    }
}
