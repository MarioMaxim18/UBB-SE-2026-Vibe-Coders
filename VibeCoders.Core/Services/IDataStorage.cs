using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VibeCoders.Models;
using Windows.Media.Protection.PlayReady;
using Windows.System;
using User = VibeCoders.Models.User;


namespace VibeCoders.Services
{
    public interface IDataStorage
    {
        bool saveUser(User u);
        User loadUser(string username);
        bool saveClientData(Client c);
        bool saveWorkoutLog(WorkoutLog log);
        List<Client> getTrainerClient(int trainerId);
    }
}
