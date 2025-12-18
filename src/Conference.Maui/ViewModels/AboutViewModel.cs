using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Conference.Maui.ViewModels;

public partial class AboutViewModel : BaseViewModel
{
    [ObservableProperty]
    private string eventName = "My Conference";

    [ObservableProperty]
    private string eventDescription = "Welcome to our conference app! This app provides you with all the information you need about sessions, speakers, and the event schedule.";

    [ObservableProperty]
    private string version = "1.0.0";

    public AboutViewModel()
    {
        Title = "About";
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        await Shell.Current.GoToAsync("settings");
    }
}
