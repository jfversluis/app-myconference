using Conference.Maui.ViewModels;

namespace Conference.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        var savedTheme = Preferences.Get("app_theme", 0);
        UserAppTheme = savedTheme switch
        {
            1 => AppTheme.Light,
            2 => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Always use AppShell — iOS requires Shell as root page
        return new Window(new AppShell());
    }
}
