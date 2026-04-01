using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;

namespace VibeCoders.Views
{
    public sealed partial class MealBuilderForm : UserControl
    {
        /// <summary>
        /// The ViewModel that owns all meal-builder state and commands.
        /// Exposed publicly so the host page (e.g. TrainerDashboardView)
        /// can subscribe to <see cref="MealBuilderViewModel.MealSaved"/>
        /// and collect finished meals into a NutritionPlan.
        /// </summary>
        public MealBuilderViewModel ViewModel { get; } = new MealBuilderViewModel();

        public MealBuilderForm()
        {
            this.InitializeComponent();
        }
    }
}
