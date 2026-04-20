using System.Collections.Generic;
using VibeCoders.Models;

namespace VibeCoders.Repositories.Interfaces
{
    public interface IRepositoryWorkoutTemplate
    {
        List<WorkoutTemplate> GetAvailableWorkouts(int clientId);

        TemplateExercise? GetTemplateExercise(int templateExerciseId);

        bool UpdateTemplateWeight(int templateExerciseId, double newWeight);

        List<string> GetAllExerciseNames();
    }
}
