using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public class ClientService
    {
        private readonly IDataStorage _storage;
        private readonly ProgressionService _progressionService;

        public ClientService(IDataStorage storage, ProgressionService progressionService)
        {
            _storage = storage;
            _progressionService = progressionService;
        }

        public void SaveSet(LoggedSet set)
        {
            Task.Run(() => _storage.SaveLoggedSet(set));
        }

        public bool FinalizeWorkout(WorkoutLog log)
        {
            try
            {
                log.Date = DateTime.Now;
                _storage.SaveWorkoutLog(log);

                _progressionService.EvaluateWorkout(log);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}