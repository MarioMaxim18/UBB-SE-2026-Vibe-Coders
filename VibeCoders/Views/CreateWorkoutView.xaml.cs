using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;

namespace VibeCoders.Views
{
    public partial class CreateWorkoutView : UserControl
    {
        public CreateWorkoutViewModel ViewModel { get; }

        public CreateWorkoutView()
        {
            ViewModel = App.GetService<CreateWorkoutViewModel>();
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
