using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using VibeCoders.Models;
using VibeCoders.Services;

namespace VibeCoders.ViewModels
{
    public class WorkoutLogViewModel : INotifyPropertyChanged
    {
        private readonly ClientService _clientService;
        private WorkoutLog _currentLog;
        private bool _isBusy;

        public event PropertyChangedEventHandler PropertyChanged;

        public WorkoutLogViewModel(ClientService clientService, WorkoutLog log)
        {
            _clientService = clientService;
            _currentLog = log;
            
            FinishWorkoutCommand = new RelayCommand(async () => await FinishWorkoutAsync());
        }

        public WorkoutLog CurrentLog
        {
            get => _currentLog;
            set { _currentLog = value; OnPropertyChanged(); }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public ICommand FinishWorkoutCommand { get; }

        private async Task FinishWorkoutAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                // this might show a red line in VS until ClientService is merged to main 
                bool success = await _clientService.FinalizeWorkoutAsync(CurrentLog);
                if (success)
                {
                    // Navigation logic here
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        public RelayCommand(Func<Task> execute) => _execute = execute;
        public bool CanExecute(object parameter) => true;
        public async void Execute(object parameter) => await _execute();
        public event EventHandler CanExecuteChanged;
    }
}