using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;

namespace VibeCoders.Services
{
    public class TrainerService
    {
       
        public IDataStorage dataStorage { get; set; }

     
        public TrainerService(IDataStorage storage)
        {
            dataStorage = storage;
        }

        
        public List<Client> getAssignedClient(int trainerId)
        {
            
            return dataStorage.getTrainerClient(trainerId);
        }

        
        public void assignWorkout(Client client, WorkoutLog workout)
        {
            throw new NotImplementedException("Workout assignment coming in Slice 2!");
        }
    }
}
