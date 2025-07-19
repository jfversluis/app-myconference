using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Models;
using Conference.Maui.Pages;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SelectedSession), nameof(SelectedSession))]
public partial class SessionDetailsViewModel : ObservableObject
{
    [ObservableProperty]
    Session? _selectedSession;

    [RelayCommand]
    private async Task GoToSpeakerDetails(Speaker selectedSpeaker)
    {
        try
        {
            // Check if we're coming from SpeakerDetailsPage to avoid circular navigation
            var navigationStack = Shell.Current.Navigation.NavigationStack;
            
            // Check if the previous page in the stack is SpeakerDetailsPage
            if (navigationStack.Count >= 2)
            {
                var previousPage = navigationStack[navigationStack.Count - 2];
                if (previousPage is SpeakerDetailsPage)
                {
                    // We came from SpeakerDetailsPage, so just go back instead of creating a loop
                    await Shell.Current.GoToAsync("..");
                    return;
                }
            }

            // Navigate forward to SpeakerDetailsPage if we didn't come from there
            await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage),
                new Dictionary<string, object> { { "SelectedSpeaker", selectedSpeaker } });
        }
        catch (Exception ex)
        {
            // Log error or handle gracefully
            System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
            
            // Fallback: just try to navigate normally
            await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage),
                new Dictionary<string, object> { { "SelectedSpeaker", selectedSpeaker } });
        }
    }
}
