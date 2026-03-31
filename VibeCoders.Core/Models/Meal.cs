using System.Collections.Generic;

namespace VibeCoders.Models
{
    public class Meal
    {
        public string name { get; set; }
        public List<string> ingredients { get; set; } = new List<string>();
        public string instructions { get; set; }
    }
}
