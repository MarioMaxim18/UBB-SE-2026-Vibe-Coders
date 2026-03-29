using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using Windows.UI.Notifications;
using System.Threading.Tasks;

namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        Task SaveWorkoutLogAsync(WorkoutLog log);
        Task SaveLoggedSetAsync(LoggedSet set);

        void UpdateTemplateWeight(int templateExId, double newWeight);
        void SaveNotification(Models.Notification n);
        TemplateExercise GetTemplateExercise(int exerciseId);
    }
}