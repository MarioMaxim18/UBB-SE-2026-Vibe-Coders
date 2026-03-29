using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public class LoggedSet
    {
        public int Id { get; set; }
        public int SetNumber { get; set; }
        public double Weight { get; set; }

        public int ActReps { get; set; }

        public int ParentTemplateExerciseId { get; set; }
    }
}