using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Configuration;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.Maui.SwipeCardView.Core;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    private const string OnboardingCompletedKey = "onboarding_completed_v1";
    private const int QuickPickCardLimit = 15;

    // Vibrant colors for speaker photo circles on the welcome screen
    private static readonly string[] CircleColors =
        ["#FF6B35", "#FFC233", "#4ECDC4", "#C44DFF", "#FF4081", "#00BCD4"];

    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<OnboardingViewModel> _logger;

    private AllDataResponse? _allData;
    private Dictionary<int, string> _mainTagMap = [];
    private readonly HashSet<string> _sessionFavoritedIds = [];
    private IReadOnlySet<string> _existingFavoriteIds = new HashSet<string>();
    private List<SessionItem> _filteredDeck = [];
    private SessionItem? _lastSwipedItem;
    private SwipeCardDirection _lastSwipeDirection;
    private int _swipedThisSession;

    // 0=Welcome, 1=Notifications, 2=Interests, 3=QuickPick, 4=Done
    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasLoadError;

    // Welcome screen speaker photos
    [ObservableProperty]
    private ObservableCollection<FeaturedSpeaker> _featuredSpeakers = [];

    public bool HasFeaturedSpeakers => FeaturedSpeakers.Count > 0;

    // Notification permission
    [ObservableProperty]
    private bool _notificationsGranted;

    [ObservableProperty]
    private bool _notificationPermissionRequested;

    // Interest selection
    [ObservableProperty]
    private ObservableCollection<InterestTag> _availableTags = [];

    public bool HasSelectedTags => AvailableTags.Any(t => t.IsSelected);
    public int SelectedTagCount => AvailableTags.Count(t => t.IsSelected);

    // Quick Pick cards
    [ObservableProperty]
    private ObservableCollection<SessionItem> _cards = [];

    [ObservableProperty]
    private int _addedCount;

    [ObservableProperty]
    private int _quickPickTotal;

    [ObservableProperty]
    private bool _showUndo;

    [ObservableProperty]
    private bool _hasConflict;

    [ObservableProperty]
    private string _conflictText = string.Empty;

    public double QuickPickProgress => QuickPickTotal > 0 ? (double)_swipedThisSession / QuickPickTotal : 0;
    public string QuickPickProgressText => QuickPickTotal > 0 ? $"{_swipedThisSession} / {QuickPickTotal}" : string.Empty;
    public bool HasCards => Cards.Count > _swipedThisSession;

    public OnboardingViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ILogger<OnboardingViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _logger = logger;
    }

    public static bool IsOnboardingCompleted()
    {
        return Preferences.Get(OnboardingCompletedKey, false);
    }

    public static void MarkOnboardingCompleted()
    {
        Preferences.Set(OnboardingCompletedKey, true);
    }

    public static void ResetOnboarding()
    {
        Preferences.Remove(OnboardingCompletedKey);
    }

    public async Task InitializeAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            HasLoadError = false;
            CurrentStep = 0;

            _allData = await _dataService.GetAllDataAsync();
            if (_allData == null)
            {
                HasLoadError = true;
                return;
            }

            BuildFeaturedSpeakers();
            _existingFavoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();
            await FetchCategoryTagsAsync();
            BuildInterestTags();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing onboarding");
            HasLoadError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BuildFeaturedSpeakers()
    {
        if (_allData?.Speakers == null) return;

        try
        {
            // Pick 5 random speakers that have profile pictures
            var speakersWithPhotos = _allData.Speakers
                .Where(s => !string.IsNullOrEmpty(s.ProfilePicture))
                .ToList();

            if (speakersWithPhotos.Count == 0) return;

            var random = new Random();
            var picked = speakersWithPhotos
                .OrderBy(_ => random.Next())
                .Take(10)
                .ToList();

            var result = new ObservableCollection<FeaturedSpeaker>();
            for (int i = 0; i < picked.Count; i++)
            {
                result.Add(new FeaturedSpeaker
                {
                    ProfilePictureUrl = picked[i].ProfilePicture ?? string.Empty,
                    CircleColor = CircleColors[i % CircleColors.Length]
                });
            }

            FeaturedSpeakers = result;
            OnPropertyChanged(nameof(HasFeaturedSpeakers));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load featured speakers for welcome screen");
        }
    }

    private async Task FetchCategoryTagsAsync()
    {
        try
        {
            using var http = new HttpClient();
            var url = $"https://sessionize.com/api/v2/{AppConfig.SessionizeApiId}/view/All";
            var json = await http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("categories", out var categories))
                return;

            foreach (var cat in categories.EnumerateArray())
            {
                var title = cat.GetProperty("title").GetString() ?? "";
                if (!title.Contains("tag", StringComparison.OrdinalIgnoreCase) ||
                    title.Contains("other", StringComparison.OrdinalIgnoreCase))
                    continue;

                // This is the "Main tag" category
                foreach (var item in cat.GetProperty("items").EnumerateArray())
                {
                    var id = item.GetProperty("id").GetInt32();
                    var name = item.GetProperty("name").GetString() ?? "";
                    _mainTagMap[id] = name;
                }
                break;
            }

            _logger.LogInformation("Loaded {Count} main tags for onboarding", _mainTagMap.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch category tags; interest selection will be skipped");
        }
    }

    private void BuildInterestTags()
    {
        if (_allData == null || _mainTagMap.Count == 0) return;

        // Count sessions per main tag (non-service only)
        var nonServiceSessions = _allData.Sessions.Where(s => !s.IsServiceSession).ToList();
        var tagCounts = new Dictionary<int, int>();
        foreach (var session in nonServiceSessions)
        {
            foreach (var catObj in session.CategoryItems)
            {
                if (catObj is not JsonElement je) continue;
                var catId = je.GetInt32();
                if (_mainTagMap.ContainsKey(catId))
                {
                    tagCounts[catId] = tagCounts.GetValueOrDefault(catId) + 1;
                }
            }
        }

        // Only show tags that have at least 2 sessions, sorted by popularity
        var tags = _mainTagMap
            .Where(kv => tagCounts.GetValueOrDefault(kv.Key) >= 2)
            .OrderByDescending(kv => tagCounts.GetValueOrDefault(kv.Key))
            .Select(kv => new InterestTag
            {
                Id = kv.Key,
                Name = kv.Value,
                SessionCount = tagCounts.GetValueOrDefault(kv.Key)
            })
            .ToList();

        AvailableTags = new ObservableCollection<InterestTag>(tags);
    }

    [RelayCommand]
    private void ToggleTag(InterestTag tag)
    {
        tag.IsSelected = !tag.IsSelected;
        OnPropertyChanged(nameof(HasSelectedTags));
        OnPropertyChanged(nameof(SelectedTagCount));
    }

    [RelayCommand]
    private void GoToStep(int step)
    {
        CurrentStep = step;
    }

    [RelayCommand]
    private async Task RequestNotificationPermissionAsync()
    {
        try
        {
            NotificationPermissionRequested = true;
            var result = await LocalNotificationCenter.Current.RequestNotificationPermission();
            NotificationsGranted = result;
            _logger.LogInformation("Notification permission result: {Granted}", result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not request notification permission");
        }
    }

    [RelayCommand]
    private async Task StartQuickPickAsync()
    {
        CurrentStep = 3; // QuickPick step
        await BuildFilteredDeckAsync();
    }

    [RelayCommand]
    private async Task SkipToAppAsync()
    {
        MarkOnboardingCompleted();
        await CloseOnboardingAsync();
    }

    [RelayCommand]
    private async Task FinishOnboardingAsync()
    {
        MarkOnboardingCompleted();
        await CloseOnboardingAsync();
    }

    private async Task CloseOnboardingAsync()
    {
        // Dismiss the modal onboarding page
        if (Application.Current?.Windows.FirstOrDefault()?.Page?.Navigation is { } nav)
        {
            await nav.PopModalAsync(animated: true);
        }
    }

    private async Task BuildFilteredDeckAsync()
    {
        if (_allData == null) return;

        try
        {
            IsBusy = true;
            _swipedThisSession = 0;
            _sessionFavoritedIds.Clear();
            AddedCount = 0;
            ShowUndo = false;
            HasConflict = false;

            var selectedTagIds = AvailableTags
                .Where(t => t.IsSelected)
                .Select(t => t.Id)
                .ToHashSet();

            var sessions = _allData.Sessions
                .Where(s => !s.IsServiceSession && !_existingFavoriteIds.Contains(s.Id))
                .ToList();

            List<SessionDetails> filtered;
            if (selectedTagIds.Count > 0)
            {
                // Score sessions by how many selected tags they match
                filtered = sessions
                    .Select(s => (Session: s, Score: GetMatchingTagCount(s, selectedTagIds)))
                    .Where(x => x.Score > 0)
                    .OrderByDescending(x => x.Score)
                    .ThenBy(x => x.Session.StartsAt)
                    .Select(x => x.Session)
                    .Take(QuickPickCardLimit)
                    .ToList();
            }
            else
            {
                // No tags selected — show a diverse mix
                filtered = sessions
                    .OrderBy(s => s.StartsAt)
                    .Take(QuickPickCardLimit)
                    .ToList();
            }

            _filteredDeck = filtered.Select(CreateSessionItem).ToList();
            QuickPickTotal = _filteredDeck.Count;
            Cards = new ObservableCollection<SessionItem>(_filteredDeck);

            if (Cards.Count > 0)
            {
                UpdateConflictForCurrentCard();
            }

            NotifyQuickPickChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building filtered deck");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task HandleSwipeAsync(SessionItem? session, SwipeCardDirection direction)
    {
        if (session == null) return;

        _lastSwipedItem = session;
        _lastSwipeDirection = direction;

        try
        {
            if (direction == SwipeCardDirection.Right)
            {
                await _favoritesService.AddFavoriteAsync(session.Id);
                session.IsFavorite = true;
                _sessionFavoritedIds.Add(session.Id);
                AddedCount++;
                WeakReferenceMessenger.Default.Send(new FavoriteChangedMessage(session.Id, true));
            }

            _swipedThisSession++;
            ShowUndo = true;
            NotifyQuickPickChanged();

            if (!HasCards)
            {
                CurrentStep = 4; // Done
            }
            else
            {
                UpdateConflictForCurrentCard();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing onboarding swipe");
        }
    }

    [RelayCommand]
    private async Task UndoLastSwipeAsync()
    {
        if (_lastSwipedItem == null) return;

        try
        {
            if (_lastSwipeDirection == SwipeCardDirection.Right)
            {
                await _favoritesService.RemoveFavoriteAsync(_lastSwipedItem.Id);
                _lastSwipedItem.IsFavorite = false;
                _sessionFavoritedIds.Remove(_lastSwipedItem.Id);
                AddedCount--;
                WeakReferenceMessenger.Default.Send(new FavoriteChangedMessage(_lastSwipedItem.Id, false));
            }

            _swipedThisSession--;
            ShowUndo = false;
            _lastSwipedItem = null;
            NotifyQuickPickChanged();
            UpdateConflictForCurrentCard();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing onboarding swipe");
        }
    }

    [RelayCommand]
    private void FinishQuickPick()
    {
        CurrentStep = 4; // Done screen
    }

    private void UpdateConflictForCurrentCard()
    {
        if (_allData == null || !HasCards) return;

        var currentSession = Cards[_swipedThisSession];
        var allFavIds = _existingFavoriteIds.Union(_sessionFavoritedIds).ToHashSet();

        var conflicting = _allData.Sessions
            .Where(s => allFavIds.Contains(s.Id)
                && s.Id != currentSession.Id
                && s.StartsAt < currentSession.EndsAt
                && s.EndsAt > currentSession.StartsAt)
            .ToList();

        HasConflict = conflicting.Count > 0;
        ConflictText = conflicting.Count switch
        {
            1 => $"⚠️ Conflicts with \"{conflicting[0].Title}\"",
            > 1 => $"⚠️ {conflicting.Count} sessions in this time slot",
            _ => string.Empty
        };
    }

    private SessionItem CreateSessionItem(SessionDetails session)
    {
        var speakers = session.Speakers
            .Select(sid => _allData?.Speakers.FirstOrDefault(s => s.Id == sid))
            .Where(s => s != null)
            .Select(s => SpeakerItem.FromSpeakerDetails(s!))
            .ToList();

        return new SessionItem
        {
            Id = session.Id,
            Title = session.Title,
            Description = session.Description,
            StartsAt = session.StartsAt,
            EndsAt = session.EndsAt,
            RoomId = session.RoomId,
            RoomName = _allData?.Rooms.FirstOrDefault(r => r.Id == session.RoomId)?.Name,
            Speakers = speakers,
            IsFavorite = false
        };
    }

    private static int GetMatchingTagCount(SessionDetails session, HashSet<int> tagIds)
    {
        int count = 0;
        foreach (var catObj in session.CategoryItems)
        {
            if (catObj is JsonElement je && tagIds.Contains(je.GetInt32()))
                count++;
        }
        return count;
    }

    private void NotifyQuickPickChanged()
    {
        OnPropertyChanged(nameof(QuickPickProgress));
        OnPropertyChanged(nameof(QuickPickProgressText));
        OnPropertyChanged(nameof(HasCards));
    }
}

public partial class InterestTag : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SessionCount { get; set; }

    [ObservableProperty]
    private bool _isSelected;
}

public class FeaturedSpeaker
{
    public string ProfilePictureUrl { get; set; } = string.Empty;
    public string CircleColor { get; set; } = "#4ECDC4";
}
