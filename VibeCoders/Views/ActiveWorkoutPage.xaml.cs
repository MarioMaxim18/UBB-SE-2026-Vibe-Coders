using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using VibeCoders.Models;
using VibeCoders.ViewModels;

namespace VibeCoders.Views;

public sealed partial class ActiveWorkoutPage : Page
{
    public ActiveWorkoutViewModel ViewModel { get; }

    /// <summary>
    /// The current client id — set during navigation via OnNavigatedTo.
    /// Bound to FinishWorkoutCommand and LoadAvailableWorkoutsCommand as CommandParameter.
    /// </summary>
    public int ClientId { get; private set; }

    public ActiveWorkoutPage()
    {
        ViewModel = App.GetService<ActiveWorkoutViewModel>();
        DataContext = ViewModel;
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is int clientId)
        {
            ClientId = clientId;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadAvailableWorkoutsCommand.Execute(ClientId);
        ViewModel.LoadNotificationsCommand.Execute(ClientId);
    }

    /// <summary>
    /// Handles Confirm Deload button click from inside DataTemplate.
    /// Tag="{x:Bind}" passes the Notification as the button's Tag.
    /// </summary>
    private void ConfirmDeloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Notification notification)
        {
            ViewModel.ConfirmDeloadCommand.Execute(notification);
        }
    }
}