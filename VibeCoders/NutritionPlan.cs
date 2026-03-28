using System;

public class NutritionPlan
{
    //Atributes
    private int _planId;
    private string _startDate;
    private string _endDate;
    private List<Meal> _meals;

    //Constructor
    public NutritionPlan()
    {
        _meals = new List<Meal>();
    }

    //Getters and Setters
    public int PlanId
    {
        get { return _planId; }
        set { _planId = value; }
    }

    public string StartDate
    {
        get { return _startDate; }
        set { _startDate = value; }
    }

    public string EndDate
    {
        get { return _endDate; }
        set { _endDate = value; }
    }

    public List<Meal> Meals
    {
        get { return _meals; }
        set { _meals = value; }
    }

    
}
