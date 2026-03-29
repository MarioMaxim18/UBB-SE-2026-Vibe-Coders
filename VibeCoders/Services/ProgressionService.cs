using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using VibeCoders.Utils;

namespace VibeCoders.Services
{
    public class ProgressionService
    {
        private readonly IDataStorage _storage;

        public ProgressionService(IDataStorage storage)
        {
            _storage = storage;
        }

        public void EvaluateWorkout(WorkoutLog log)
        {
            foreach (var exercise in log.Exercises)
            {
                EvaluateExercise(exercise);
            }
        }

        private void EvaluateExercise(LoggedExercise exercise)
        {
            if (exercise.Sets == null || exercise.Sets.Count == 0) return;

            TemplateExercise template = _storage.GetTemplateExercise(exercise.Id);
            if (template == null) return;

            bool isOverload = true;
            int plateauCount = 0;
            double lastRatio = 0.0;
            double currentWeight = exercise.Sets[0].Weight;

            foreach (var set in exercise.Sets)
            {
                double ratio = ProgressionUtils.CalculateRatio(set.ActReps, template.TargetReps);
                exercise.PerformanceRatio = ratio;

                if (ratio < 1.0)
                {
                    isOverload = false;
                }

                if (ratio < 0.9)
                {
                    plateauCount++;
                }
                else
                {
                    plateauCount = 0;
                }

                lastRatio = ratio;
            }

            if (isOverload)
            {
                double increment = ProgressionUtils.DetermineWeightIncrement(template.MuscleGroup);
                double newWeight = currentWeight + increment;

                _storage.UpdateTemplateWeight(template.Id, newWeight);

                exercise.IsSystemAdjusted = true;
                exercise.AdjustmentNote = $"Previous: {currentWeight}kg -> New: {newWeight}kg (Performance Ratio {lastRatio})";
            }
            else if (plateauCount >= 2)
            {
                var notification = new Notification(
                    "Deload Recommended",
                    $"Plateau detected for {exercise.ExerciseName}.",
                    NotificationType.PLATEAU,
                    template.Id
                );

                _storage.SaveNotification(notification);

                exercise.IsSystemAdjusted = true;
                exercise.AdjustmentNote = "Plateau detected. Deload notification sent.";
            }
        }
    }
}