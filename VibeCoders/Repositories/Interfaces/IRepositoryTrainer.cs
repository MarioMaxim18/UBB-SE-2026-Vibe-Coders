namespace VibeCoders.Repositories.Interfaces
{
    using VibeCoders.Models;
    using User = VibeCoders.Models.User;

    public interface IRepositoryTrainer
    {
        List<Client> GetTrainerClients(int trainerId);

        bool SaveTrainerWorkout(WorkoutTemplate template);

        bool DeleteWorkoutTemplate(int templateId);

        bool SaveUser(User user);

        User? LoadUser(string username);

        bool SaveClientData(Client client);
    }
}
