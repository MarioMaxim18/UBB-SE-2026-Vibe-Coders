namespace VibeCoders.Models;

/// <summary>
/// A single meal belonging to a <see cref="NutritionPlan"/>.
/// <see cref="Ingredients"/> is stored as a JSON-serialized list in the
/// <c>MEAL.ingredients</c> column (VARCHAR MAX).
/// </summary>
public class Meal
{
    public int         MealId          { get; set; }
    public int         NutritionPlanId { get; set; }
    public string      Name            { get; set; } = string.Empty;

    /// <summary>
    /// List of ingredient strings (e.g. "200g chicken breast").
    /// Serialized to / deserialized from JSON when persisted in SQL.
    /// </summary>
    public List<string> Ingredients    { get; set; } = new();

    public string      Instructions    { get; set; } = string.Empty;
}
