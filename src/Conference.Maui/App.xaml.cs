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
        // Use async fire-and-forget without Task.Run to avoid threading issues
        _ = InitializeDataSyncSafely();

        return window;
    }

    private async Task InitializeDataSyncSafely()
    {
        try
        {
            // Add a small delay to ensure app is fully initialized
            await Task.Delay(100);
            
            if (_dataSyncService != null)
            {
                await _dataSyncService.InitializeAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize data sync service: {ex.Message}");
        }
    }
}
