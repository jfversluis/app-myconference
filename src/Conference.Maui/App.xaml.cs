using Conference.Maui.Interfaces;

namespace Conference.Maui;

public partial class App : Application
{
    public App(IDatabaseService databaseService)
    {
        InitializeComponent();
        
        // Initialize database on app startup
        Task.Run(async () => await databaseService.InitializeAsync());
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
