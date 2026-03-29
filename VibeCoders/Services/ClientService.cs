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

        public async Task SaveSetAsync(LoggedSet set)
        {
            try
            {
                await _storage.SaveLoggedSetAsync(set);
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> FinalizeWorkoutAsync(WorkoutLog log)
        {
            if (log == null || log.Exercises == null) return false;

            try
            {
                log.Date = DateTime.Now;

                _progressionService.EvaluateWorkout(log);

                await _storage.SaveWorkoutLogAsync(log);

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}