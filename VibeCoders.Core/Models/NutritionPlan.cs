namespace VibeCoders.Models;

/// <summary>
/// A diet/nutrition plan assigned by a trainer to a specific client.
/// <see cref="StartDate"/> and <see cref="EndDate"/> define the active window
/// captured by the two DatePicker controls in the assignment UI (#119).
/// </summary>
public class NutritionPlan
{
    public int      PlanId    { get; set; }
    public int      ClientId  { get; set; }
    public int      TrainerId { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate   { get; set; } = DateTime.Today.AddDays(30);
    public string   Notes     { get; set; } = string.Empty;
}
