using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using VibeCoders.Services;
using VibeCoders.ViewModels;

namespace VibeCoders;

public partial class App : Application
{
    private static IServiceProvider? _services;
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _services = services.BuildServiceProvider();

        // Ensure DB schema and seed prebuilt workouts on startup.
        var storage = _services.GetRequiredService<IDataStorage>();
        if (storage is SqlDataStorage sqlStorage)
        {
            sqlStorage.EnsureSchemaCreated();
            sqlStorage.SeedPrebuiltWorkouts();
        }

        var navService = (NavigationService)_services.GetRequiredService<INavigationService>();
        _window = new MainWindow(navService);
        _window.Activate();
        navService.NavigateToClientDashboard(requestRefresh: true);
    }

    /// <summary>
    /// Resolves a service from the DI container. Used by pages that cannot
    /// receive constructor injection (WinUI page activation).
    /// </summary>
    public static T GetService<T>() where T : notnull
    {
        if (_services is null)
        {
            throw new InvalidOperationException(
                "Service provider is not initialized. Ensure OnLaunched has run.");
        }
        return _services.GetRequiredService<T>();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = DatabasePaths.GetAnalyticsDatabasePath();

        // Storage
        services.AddSingleton<IDataStorage, SqlDataStorage>();
        services.AddSingleton<IWorkoutAnalyticsStore>(
            new SqlWorkoutAnalyticsStore(dbPath));

        // Session & buses
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<IAnalyticsDashboardRefreshBus, AnalyticsDashboardRefreshBus>();
        services.AddSingleton<IWorkoutDataForwarder, WorkoutDataForwarder>();

        // Navigation
        services.AddSingleton<INavigationService, NavigationService>();

        // Services
        services.AddSingleton<ProgressionService>();
        services.AddSingleton<ClientService>();

        // ViewModels
        services.AddTransient<ClientDashboardViewModel>();
        services.AddTransient<ActiveWorkoutViewModel>();
    }
}