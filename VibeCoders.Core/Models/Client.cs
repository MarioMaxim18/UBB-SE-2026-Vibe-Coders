using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VibeCoders.Models
{
    public class Client : User
    {
        public double weight { get; set; }
        public double height { get; set; }
        public List<WorkoutLog> workoutLog { get; set; } = new List<WorkoutLog>();

        public void setWorkout(WorkoutLog workout)
        {
            
        }

        public void modifyWorkout(WorkoutLog oldWorkout, WorkoutLog newWorkout)
        {
            
        }

    }
}
