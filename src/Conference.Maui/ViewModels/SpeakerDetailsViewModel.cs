using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;
using Conference.Maui.Pages;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SelectedSpeaker), nameof(SelectedSpeaker))]
public partial class SpeakerDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    Speaker? _selectedSpeaker;

    [RelayCommand]
    private async Task GoToSessionDetails(Session selectedSession)
    {
        try
        {
            // Check if we're coming from SessionDetailsPage to avoid circular navigation
            var navigationStack = Shell.Current.Navigation.NavigationStack;

            // Check if the previous page in the stack is SessionDetailsPage
            if (navigationStack.Count >= 2)
            {
                var previousPage = navigationStack[navigationStack.Count - 2];
                if (previousPage is SessionDetailsPage)
                {
                    // We came from SessionDetailsPage, so just go back instead of creating a loop
                    await Shell.Current.GoToAsync("..");
                    return;
                }
            }

            // Navigate forward to SessionDetailsPage if we didn't come from there
            await NavigateToSessionDetails(selectedSession);
        }
        catch (Exception ex)
        {
            // Log error or handle gracefully
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");

            // Fallback: just try to navigate normally
            await NavigateToSessionDetails(selectedSession);
        }
    }
    
    private async Task NavigateToSessionDetails(Session selectedSession)
    {
        await Shell.Current.GoToAsync(nameof(SessionDetailsPage),
            new Dictionary<string, object> { { "SelectedSession", selectedSession } });
    }
}
