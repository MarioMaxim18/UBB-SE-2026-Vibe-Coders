namespace VibeCoders.Models;

/// <summary>
/// A diet/nutrition plan assigned by a trainer to a specific client.
/// <see cref="StartDate"/> and <see cref="EndDate"/> define the active window
/// and map directly to the <c>NUTRITION_PLAN.start_date</c> /
/// <c>NUTRITION_PLAN.end_date</c> columns.
/// <see cref="Meals"/> holds all <see cref="Meal"/> rows linked via
/// <c>MEAL.nutrition_plan_id</c>.
/// </summary>
public class NutritionPlan
{
    public int         PlanId    { get; set; }
    public DateTime    StartDate { get; set; } = DateTime.Today;
    public DateTime    EndDate   { get; set; } = DateTime.Today.AddDays(30);

    /// <summary>All meals that belong to this plan.</summary>
    public List<Meal>  Meals     { get; set; } = new();
}
