using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class FocusModeView : Page
{
    public ActiveWorkoutViewModel ViewModel { get; }

    public FocusModeView(ActiveWorkoutViewModel vm)
    {
        InitializeComponent();
        ViewModel = vm;
        DataContext = ViewModel;
    }
}