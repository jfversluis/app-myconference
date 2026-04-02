using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Conference.Maui.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Accessibility;

namespace Conference.Maui.ViewModels;

[QueryProperty(nameof(SessionId), "SessionId")]
[QueryProperty(nameof(SourceSpeakerId), "SourceSpeakerId")]
public partial class SessionDetailsViewModel : BaseViewModel, IRecipient<FavoriteChangedMessage>
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly IReminderService _reminderService;
    private readonly ILogger<SessionDetailsViewModel> _logger;

    [ObservableProperty]
    private string _sessionId = string.Empty;

    /// <summary>
    /// When navigating from a SpeakerDetailsPage, this holds the source speaker ID
    /// so we can pop back instead of pushing forward if the user taps that same speaker.
    /// </summary>
    [ObservableProperty]
    private string _sourceSpeakerId = string.Empty;

    [ObservableProperty]
    private SessionItem? _session;

    [ObservableProperty]
    private bool _isReminderActive;

    [ObservableProperty]
    private bool _showReminderToggle;

    public SessionDetailsViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        IReminderService reminderService,
        ILogger<SessionDetailsViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _reminderService = reminderService;
        _logger = logger;
        Title = "Session";

        WeakReferenceMessenger.Default.Register<FavoriteChangedMessage>(this);
    }

    partial void OnSessionIdChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            _ = LoadSessionAsync();
        }
    }

    private async Task LoadSessionAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            var allData = await _dataService.GetAllDataAsync();
            var sessionData = allData?.Sessions.FirstOrDefault(s => s.Id == SessionId);
            
            if (sessionData != null)
            {
                var favorites = await _favoritesService.GetFavoriteSessionIdsAsync();
                var speakers = sessionData.Speakers
                    .Select(speakerId => allData?.Speakers.FirstOrDefault(s => s.Id == speakerId))
                    .Where(s => s != null)
                    .Select(s => SpeakerItem.FromSpeakerDetails(s!))
                    .ToList();

                Session = new SessionItem
                {
                    Id = sessionData.Id,
                    Title = sessionData.Title,
                    Description = sessionData.Description,
                    StartsAt = sessionData.StartsAt,
                    EndsAt = sessionData.EndsAt,
                    RoomName = allData?.Rooms.FirstOrDefault(r => r.Id == sessionData.RoomId)?.Name,
                    RoomId = sessionData.RoomId,
                    Speakers = speakers,
                    IsFavorite = favorites.Contains(sessionData.Id)
                };

                Title = Session.Title;
                await UpdateReminderStateAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading session details for {SessionId}", SessionId);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateReminderStateAsync()
    {
        if (Session == null) return;

        try
        {
#if DEBUG
            // In debug builds, treat all sessions as future so reminders UI is testable with past conference data
            var isFuture = true;
#else
            var isFuture = Session.StartsAt > DateTimeOffset.Now;
#endif
            ShowReminderToggle = Session.IsFavorite && _reminderService.IsGlobalRemindersEnabled && isFuture;
            IsReminderActive = ShowReminderToggle && await _reminderService.IsReminderActiveAsync(Session.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reminder state");
            ShowReminderToggle = false;
        }
    }

    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Session == null) return;

        Session.IsFavorite = await _favoritesService.ToggleFavoriteAsync(Session.Id);
        await UpdateReminderStateAsync();

        SemanticScreenReader.Announce(Session.IsFavorite ? "Added to favorites" : "Removed from favorites");

        if (HapticService.IsEnabled)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Haptics not available: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task ToggleReminderAsync()
    {
        if (Session == null) return;

        IsReminderActive = await _reminderService.ToggleSessionReminderAsync(
            Session.Id, Session.Title, Session.RoomName, Session.StartsAt);

        SemanticScreenReader.Announce(IsReminderActive ? "Reminder enabled" : "Reminder disabled");

        if (HapticService.IsEnabled)
        {
            try
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Haptics not available: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task NavigateToSpeakerAsync(SpeakerItem speaker)
    {
        // If we came from this speaker, pop back instead of creating a loop
        if (!string.IsNullOrEmpty(SourceSpeakerId) && speaker.Id == SourceSpeakerId)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync(nameof(SpeakerDetailsPage), new Dictionary<string, object>
        {
            ["SpeakerId"] = speaker.Id,
            ["SourceSessionId"] = SessionId
        });
    }

    public void Receive(FavoriteChangedMessage message)
    {
        if (Session != null && Session.Id == message.SessionId)
        {
            Session.IsFavorite = message.IsFavorite;
            _ = UpdateReminderStateAsync();
        }
    }
}
