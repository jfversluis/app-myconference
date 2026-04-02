using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Akavache;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Accessibility;
using Plugin.Maui.SwipeCardView.Core;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.ViewModels;

public partial class QuickPickViewModel : ObservableObject, IRecipient<FavoriteChangedMessage>
{
    private const string SwipeStateCacheKey = "quick_pick_swipe_state";

    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ISessionItemMapper _mapper;
    private readonly ILogger<QuickPickViewModel> _logger;

    private AllDataResponse? _allData;
    private IReadOnlySet<string> _favoriteIds = new HashSet<string>();
    // Tracks sessions favorited during THIS Quick Pick session for live conflict detection
    private readonly HashSet<string> _sessionFavoritedIds = [];
    private SwipeState _swipeState = new();
    private List<SessionItem> _fullDeck = [];
    private SessionItem? _lastSwipedItem;
    private SwipeCardDirection _lastSwipeDirection;

    [ObservableProperty]
    private ObservableCollection<SessionItem> _cards = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingCount))]
    [NotifyPropertyChangedFor(nameof(RemainingText))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    [NotifyPropertyChangedFor(nameof(HasCards))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingCount))]
    [NotifyPropertyChangedFor(nameof(RemainingText))]
    [NotifyPropertyChangedFor(nameof(Progress))]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    [NotifyPropertyChangedFor(nameof(HasCards))]
    private int _swipedCount;

    [ObservableProperty]
    private int _addedCount;

    [ObservableProperty]
    private int _skippedCount;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCards))]
    private bool _isComplete;

    [ObservableProperty]
    private bool _showUndo;

    [ObservableProperty]
    private bool _hasConflict;

    [ObservableProperty]
    private string _conflictText = string.Empty;

    [ObservableProperty]
    private bool _hasLoadError;

    [ObservableProperty]
    private int _totalConflictCount;

    [ObservableProperty]
    private string _conflictSummaryText = string.Empty;

    public bool HasConflictsOnComplete => TotalConflictCount > 0;

    public int RemainingCount => TotalCount - SwipedCount;
    public double Progress => TotalCount > 0 ? (double)SwipedCount / TotalCount : 0;
    public string ProgressText => TotalCount > 0 ? $"{SwipedCount} / {TotalCount}" : string.Empty;
    public string RemainingText => RemainingCount > 0 ? $"{RemainingCount} sessions left" : string.Empty;
    public bool HasCards => RemainingCount > 0 && !IsComplete;

    public QuickPickViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ISessionItemMapper mapper,
        ILogger<QuickPickViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _mapper = mapper;
        _logger = logger;

        WeakReferenceMessenger.Default.Register<FavoriteChangedMessage>(this);
    }

    public async Task LoadDeckAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsComplete = false;
            ShowUndo = false;
            HasLoadError = false;
            HasConflict = false;
            _sessionFavoritedIds.Clear();
            _swipedThisSession = 0;

            _logger.LogInformation("Loading Quick Pick deck...");

            _allData = await _dataService.GetAllDataAsync();
            if (_allData == null)
            {
                _logger.LogWarning("No conference data available");
                HasLoadError = true;
                return;
            }

            _mapper.Initialize(_allData);

            _favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();
            _swipeState = await LoadSwipeStateAsync();

            _logger.LogInformation(
                "Loaded state: {FavCount} favorites, {SkipCount} skipped, {TotalSessions} total sessions",
                _favoriteIds.Count, _swipeState.SkippedSessionIds.Count, _allData.Sessions.Count);

            // Build the deck: exclude service sessions, already-favorited, and already-skipped
            var sessions = _allData.Sessions
                .Where(s => !s.IsServiceSession
                    && !_favoriteIds.Contains(s.Id)
                    && !_swipeState.SkippedSessionIds.Contains(s.Id))
                .OrderBy(s => s.StartsAt)
                .ThenBy(s => s.Title)
                .ToList();

            _fullDeck = sessions.Select(s => _mapper.MapSession(s)).ToList();

            // Track totals including already-processed items
            var allNonService = _allData.Sessions.Count(s => !s.IsServiceSession);
            TotalCount = allNonService;
            AddedCount = _favoriteIds.Count;
            SkippedCount = _swipeState.SkippedSessionIds.Count;
            SwipedCount = AddedCount + SkippedCount;

            Cards = new ObservableCollection<SessionItem>(_fullDeck);

            _logger.LogInformation("Deck built: {CardCount} cards remaining, {Remaining} by count",
                Cards.Count, RemainingCount);

            if (RemainingCount <= 0)
            {
                IsComplete = true;
                CountAllConflicts();
            }
            else
            {
                // Proactively check for conflicts on the first card
                UpdateConflictForCurrentCard();
            }

            NotifyProgressChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Quick Pick deck");
            HasLoadError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Called from code-behind Swiped event handler.
    /// This bypasses the broken XAML SwipedCommand binding that can't resolve
    /// SwipedCardEventArgs from the plugin assembly with compiled bindings.
    /// </summary>
    public async Task HandleSwipeAsync(SessionItem? session, SwipeCardDirection direction)
    {
        if (session == null) return;

        _lastSwipedItem = session;
        _lastSwipeDirection = direction;

        try
        {
            switch (direction)
            {
                case SwipeCardDirection.Right:
                    await _favoritesService.AddFavoriteAsync(session.Id);
                    session.IsFavorite = true;
                    _sessionFavoritedIds.Add(session.Id);
                    AddedCount++;
                    _logger.LogDebug("Added session to favorites: {Title}", session.Title);
                    SemanticScreenReader.Announce($"Added {session.Title} to favorites");
                    break;

                case SwipeCardDirection.Left:
                    _swipeState.SkippedSessionIds.Add(session.Id);
                    SkippedCount++;
                    _logger.LogDebug("Skipped session: {Title}", session.Title);
                    SemanticScreenReader.Announce($"Skipped {session.Title}");
                    break;
            }

            SwipedCount++;
            _swipedThisSession++;
            ShowUndo = true;
            await SaveSwipeStateAsync();
            NotifyProgressChanged();

            if (RemainingCount <= 0)
            {
                IsComplete = true;
                ShowUndo = false;
                HasConflict = false;
                CountAllConflicts();
                OnPropertyChanged(nameof(HasCards));
                _logger.LogInformation("Quick Pick complete: {Added} added, {Skipped} skipped, {Conflicts} conflicts",
                    AddedCount, SkippedCount, TotalConflictCount);
                SemanticScreenReader.Announce("All sessions reviewed");
            }
            else
            {
                // Proactively check conflict for the NEXT card now showing
                UpdateConflictForCurrentCard();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing swipe for session {SessionId}", session.Id);
        }
    }

    // Tracks how many cards have been swiped in THIS session (for tracking current card index)
    private int _swipedThisSession;

    /// <summary>
    /// Proactively checks for conflicts on the current top card.
    /// Called after each swipe and on initial deck load so the user
    /// sees the warning BEFORE they start swiping.
    /// </summary>
    public void UpdateConflictForCurrentCard()
    {
        if (_allData == null || Cards.Count == 0) return;

        if (_swipedThisSession >= Cards.Count)
        {
            HasConflict = false;
            return;
        }

        var currentSession = Cards[_swipedThisSession];
        CheckForConflict(currentSession);
    }

    /// <summary>
    /// Checks for time conflicts between the given session and all favorited sessions
    /// (both pre-existing favorites AND sessions favorited during this Quick Pick session).
    /// </summary>
    public void CheckForConflict(SessionItem session)
    {
        if (_allData == null) return;

        var conflicting = _allData.Sessions
            .Where(s => (_favoriteIds.Contains(s.Id) || _sessionFavoritedIds.Contains(s.Id))
                && s.Id != session.Id
                && s.StartsAt < session.EndsAt
                && s.EndsAt > session.StartsAt)
            .ToList();

        if (conflicting.Count == 1)
        {
            var c = conflicting.First();
            HasConflict = true;
            ConflictText = $"⚠️ Conflicts with \"{c.Title}\" at {c.StartsAt:h:mm tt}";
        }
        else if (conflicting.Count > 1)
        {
            HasConflict = true;
            ConflictText = $"⚠️ Already {conflicting.Count} sessions favorited in this time slot";
        }
        else
        {
            HasConflict = false;
        }
    }

    /// <summary>
    /// Called from code-behind when dragging finishes without completing a swipe.
    /// </summary>
    public void ClearConflict()
    {
        // Don't clear — we want proactive conflict info to persist.
        // Only clear if there is no actual conflict on the current card.
    }

    /// <summary>
    /// Counts all time-slot conflicts among favorited sessions for the completion summary.
    /// </summary>
    private void CountAllConflicts()
    {
        if (_allData == null) return;

        var allFavIds = _favoriteIds.Union(_sessionFavoritedIds).ToHashSet();
        var favSessions = _allData.Sessions
            .Where(s => allFavIds.Contains(s.Id))
            .OrderBy(s => s.StartsAt)
            .ToList();

        var conflictingIds = new HashSet<string>();
        for (int i = 0; i < favSessions.Count; i++)
        {
            for (int j = i + 1; j < favSessions.Count; j++)
            {
                if (favSessions[i].StartsAt < favSessions[j].EndsAt &&
                    favSessions[i].EndsAt > favSessions[j].StartsAt)
                {
                    conflictingIds.Add(favSessions[i].Id);
                    conflictingIds.Add(favSessions[j].Id);
                }
            }
        }

        TotalConflictCount = conflictingIds.Count;
        ConflictSummaryText = TotalConflictCount switch
        {
            0 => string.Empty,
            1 => "1 session has a time conflict",
            _ => $"{TotalConflictCount} sessions have time conflicts"
        };
        OnPropertyChanged(nameof(HasConflictsOnComplete));
    }

    [RelayCommand]
    private async Task ReviewConflictsAsync()
    {
        await Shell.Current.GoToAsync(nameof(Pages.ConflictResolverPage));
    }

    [RelayCommand]
    private async Task UndoLastSwipeAsync()
    {
        if (_lastSwipedItem == null) return;

        try
        {
            switch (_lastSwipeDirection)
            {
                case SwipeCardDirection.Right:
                    await _favoritesService.RemoveFavoriteAsync(_lastSwipedItem.Id);
                    _lastSwipedItem.IsFavorite = false;
                    _sessionFavoritedIds.Remove(_lastSwipedItem.Id);
                    AddedCount--;
                    _logger.LogDebug("Undid favorite: {Title}", _lastSwipedItem.Title);
                    break;

                case SwipeCardDirection.Left:
                    _swipeState.SkippedSessionIds.Remove(_lastSwipedItem.Id);
                    SkippedCount--;
                    _logger.LogDebug("Undid skip: {Title}", _lastSwipedItem.Title);
                    break;
            }

            SwipedCount--;
            _swipedThisSession--;
            IsComplete = false;
            ShowUndo = false;
            _lastSwipedItem = null;

            await SaveSwipeStateAsync();
            NotifyProgressChanged();
            UpdateConflictForCurrentCard();

            SemanticScreenReader.Announce("Undo complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing last swipe");
        }
    }

    [RelayCommand]
    private async Task ResetSkippedAsync()
    {
        _swipeState.SkippedSessionIds.Clear();
        await SaveSwipeStateAsync();
        await LoadDeckAsync();
    }

    [RelayCommand]
    private async Task StartOverAsync()
    {
        var page = Application.Current?.Windows.FirstOrDefault()?.Page;
        if (page == null) return;

        var confirmed = await page.DisplayAlertAsync(
            "Start Over",
            "This will remove all favorites added through Quick Pick and reset your progress. You'll start fresh with all sessions.",
            "Start Over", "Cancel");

        if (!confirmed) return;

        // Remove all favorites
        foreach (var id in _favoriteIds)
        {
            await _favoritesService.ToggleFavoriteAsync(id);
        }

        // Clear skipped state
        _swipeState.SkippedSessionIds.Clear();
        await SaveSwipeStateAsync();

        _logger.LogInformation("Quick Pick fully reset: cleared {FavCount} favorites and all skipped sessions", _favoriteIds.Count);

        await LoadDeckAsync();
    }

    [RelayCommand]
    private async Task ViewAgendaAsync()
    {
        await Shell.Current.GoToAsync("//Favorites");
    }

    [RelayCommand]
    private async Task NavigateToSessionAsync(string sessionId)
    {
        await Shell.Current.GoToAsync("SessionDetailsPage", new Dictionary<string, object>
        {
            ["SessionId"] = sessionId
        });
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    public void Receive(FavoriteChangedMessage message)
    {
        var card = Cards.FirstOrDefault(c => c.Id == message.SessionId);
        if (card != null)
        {
            card.IsFavorite = message.IsFavorite;
        }
    }

    private void NotifyProgressChanged()
    {
        OnPropertyChanged(nameof(RemainingCount));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(RemainingText));
        OnPropertyChanged(nameof(HasCards));
    }

    private async Task<SwipeState> LoadSwipeStateAsync()
    {
        try
        {
            var state = await BlobCache.UserAccount.GetObject<SwipeState>(SwipeStateCacheKey);
            _logger.LogDebug("Loaded swipe state: {SkipCount} skipped, last updated {Updated}",
                state.SkippedSessionIds.Count, state.LastUpdated);
            return state;
        }
        catch
        {
            _logger.LogDebug("No existing swipe state found, starting fresh");
            return new SwipeState();
        }
    }

    private async Task SaveSwipeStateAsync()
    {
        _swipeState.LastUpdated = DateTime.UtcNow;
        await BlobCache.UserAccount.InsertObject(SwipeStateCacheKey, _swipeState);
    }
}
