using Microsoft.UI.Xaml.Controls;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class FocusModeView : Page
{
    private readonly ContentDialog _hostDialog;
    private readonly int _clientId;

    public ActiveWorkoutViewModel ViewModel { get; }

    public FocusModeView(ActiveWorkoutViewModel vm, int clientId, ContentDialog hostDialog)
    {
        InitializeComponent();
        ViewModel = vm;
        DataContext = vm;
        _clientId = clientId;
        _hostDialog = hostDialog;
    }

    private void FinishButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.FinishWorkoutCommand.Execute(_clientId);
        if (!ViewModel.IsWorkoutStarted && string.IsNullOrEmpty(ViewModel.ErrorMessage))
            _hostDialog.Hide();
    }
}
