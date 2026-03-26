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
        return new Window(new AppShell());
    }
}
