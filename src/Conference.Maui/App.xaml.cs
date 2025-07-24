using Conference.Maui.Services;

namespace Conference.Maui;

public partial class App : Application
{
    private readonly DataSyncService _dataSyncService;

    public App(DataSyncService dataSyncService)
    {
        InitializeComponent();
        _dataSyncService = dataSyncService;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());

        // Initialize the data sync service when the app starts
        _ = Task.Run(async () =>
        {
            try
            {
                if (_dataSyncService != null)
                {
                    await _dataSyncService.InitializeAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to initialize data sync service: {ex.Message}");
            }
        });

        return window;
    }
}
