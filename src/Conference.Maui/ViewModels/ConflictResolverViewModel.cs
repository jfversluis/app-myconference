using System.Collections.ObjectModel;
using System.Reactive.Linq;
using Akavache;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Accessibility;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.ViewModels;

public partial class ConflictResolverViewModel : ObservableObject
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ISessionItemMapper _mapper;
    private readonly ILogger<ConflictResolverViewModel> _logger;
    private AllDataResponse? _allData;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasLoaded;

    [ObservableProperty]
    private int _totalConflicts;

    [ObservableProperty]
    private int _resolvedCount;

    [ObservableProperty]
    private bool _canUndo;

    // Undo state
    private ConflictGroup? _lastResolvedGroup;
    private SessionItem? _lastKeptSession;
    private List<string>? _lastRemovedIds;

    public ObservableCollection<ConflictGroup> ConflictGroups { get; } = [];

    public bool AllResolved => ConflictGroups.Count == 0 && !IsBusy && HasLoaded;

    public ConflictResolverViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ISessionItemMapper mapper,
        ILogger<ConflictResolverViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task LoadConflictsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        HasLoaded = false;
        CanUndo = false;
        OnPropertyChanged(nameof(AllResolved));

        try
        {
            _allData = await _dataService.GetAllDataAsync();
            if (_allData == null) return;

            _mapper.Initialize(_allData);

            var favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();

            var favSessions = _allData.Sessions
                .Where(s => favoriteIds.Contains(s.Id))
                .OrderBy(s => s.StartsAt)
                .Select(s => _mapper.MapSession(s))
                .ToList();

            foreach (var s in favSessions)
                s.IsFavorite = true;

            var processed = new HashSet<string>();
            var groups = new List<ConflictGroup>();

            foreach (var session in favSessions)
            {
                if (processed.Contains(session.Id)) continue;

                var overlapping = favSessions
                    .Where(s => s.Id != session.Id
                        && !processed.Contains(s.Id)
                        && s.StartsAt < session.EndsAt
                        && s.EndsAt > session.StartsAt)
                    .ToList();

                if (overlapping.Count == 0)
                {
                    processed.Add(session.Id);
                    continue;
                }

                var groupSessions = new List<SessionItem> { session };
                groupSessions.AddRange(overlapping);

                // Transitive expansion
                bool found;
                do
                {
                    found = false;
                    foreach (var gs in groupSessions.ToList())
                    {
                        var additional = favSessions
                            .Where(s => !groupSessions.Contains(s)
                                && !processed.Contains(s.Id)
                                && s.StartsAt < gs.EndsAt
                                && s.EndsAt > gs.StartsAt)
                            .ToList();
                        if (additional.Count > 0)
                        {
                            groupSessions.AddRange(additional);
                            found = true;
                        }
                    }
                } while (found);

                groupSessions = groupSessions.Distinct().OrderBy(s => s.StartsAt).ToList();

                foreach (var s in groupSessions)
                    processed.Add(s.Id);

                var timeSlotLabel = $"{session.StartsAt.LocalDateTime:ddd, MMM d \u00b7 h:mm tt}";
                var group = new ConflictGroup(timeSlotLabel, groupSessions);
                groups.Add(group);
            }

            ConflictGroups.Clear();
            foreach (var g in groups)
                ConflictGroups.Add(g);

            TotalConflicts = groups.Count;
            ResolvedCount = 0;

            _logger.LogInformation("Found {Count} conflict groups to resolve", groups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading conflicts");
        }
        finally
        {
            IsBusy = false;
            HasLoaded = true;
            OnPropertyChanged(nameof(AllResolved));
        }
    }

    [RelayCommand]
    private async Task KeepSessionAsync(SessionItem session)
    {
        var group = ConflictGroups.FirstOrDefault(g => g.Sessions.Contains(session));
        if (group == null) return;

        try
        {
            var removedIds = new List<string>();
            foreach (var other in group.Sessions.Where(s => s.Id != session.Id))
            {
                await _favoritesService.RemoveFavoriteAsync(other.Id);
                other.IsFavorite = false;
                removedIds.Add(other.Id);
                _logger.LogDebug("Unfavorited conflicting session: {Title}", other.Title);
            }

            await MarkSessionsAsSkippedAsync(removedIds);

            session.IsFavorite = true;

            // Store undo state before removing the group
            _lastResolvedGroup = group;
            _lastKeptSession = session;
            _lastRemovedIds = removedIds;

            ConflictGroups.Remove(group);
            ResolvedCount++;
            CanUndo = true;

            OnPropertyChanged(nameof(AllResolved));

            _logger.LogInformation("Kept \"{Title}\", removed {Count} conflicts",
                session.Title, group.Sessions.Count - 1);

            SemanticScreenReader.Announce($"Kept {session.Title}");

            if (AllResolved)
                SemanticScreenReader.Announce("All conflicts resolved. Your agenda is now conflict-free.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving conflict for session {Id}", session.Id);
        }
    }

    [RelayCommand]
    private async Task UndoAsync()
    {
        if (_lastResolvedGroup == null || _lastRemovedIds == null) return;

        try
        {
            // Re-favorite the removed sessions
            foreach (var id in _lastRemovedIds)
            {
                await _favoritesService.ToggleFavoriteAsync(id);
                var session = _lastResolvedGroup.Sessions.FirstOrDefault(s => s.Id == id);
                if (session != null) session.IsFavorite = true;
            }

            // Remove them from skipped state
            await RemoveSessionsFromSkippedAsync(_lastRemovedIds);

            // Re-insert the group at its original sorted position
            var insertIndex = 0;
            for (var i = 0; i < ConflictGroups.Count; i++)
            {
                if (string.Compare(ConflictGroups[i].TimeSlotLabel, _lastResolvedGroup.TimeSlotLabel, StringComparison.Ordinal) > 0)
                    break;
                insertIndex = i + 1;
            }
            ConflictGroups.Insert(insertIndex, _lastResolvedGroup);
            ResolvedCount--;

            _logger.LogInformation("Undid resolution of group: {TimeSlot}", _lastResolvedGroup.TimeSlotLabel);

            _lastResolvedGroup = null;
            _lastKeptSession = null;
            _lastRemovedIds = null;
            CanUndo = false;

            OnPropertyChanged(nameof(AllResolved));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error undoing conflict resolution");
        }
    }

    private const string SwipeStateCacheKey = "quick_pick_swipe_state";

    private async Task MarkSessionsAsSkippedAsync(List<string> sessionIds)
    {
        try
        {
            SwipeState state;
            try
            {
                state = await BlobCache.UserAccount.GetObject<SwipeState>(SwipeStateCacheKey);
            }
            catch
            {
                state = new SwipeState();
            }

            foreach (var id in sessionIds)
            {
                if (!state.SkippedSessionIds.Contains(id))
                    state.SkippedSessionIds.Add(id);
            }

            state.LastUpdated = DateTime.UtcNow;
            await BlobCache.UserAccount.InsertObject(SwipeStateCacheKey, state);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update swipe state with skipped sessions");
        }
    }

    private async Task RemoveSessionsFromSkippedAsync(List<string> sessionIds)
    {
        try
        {
            var state = await BlobCache.UserAccount.GetObject<SwipeState>(SwipeStateCacheKey);
            foreach (var id in sessionIds)
                state.SkippedSessionIds.Remove(id);
            state.LastUpdated = DateTime.UtcNow;
            await BlobCache.UserAccount.InsertObject(SwipeStateCacheKey, state);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove sessions from skipped state");
        }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    [RelayCommand]
    private async Task ViewAgendaAsync()
    {
        await Shell.Current.GoToAsync("//Favorites");
    }
}

public class ConflictGroup
{
    public string TimeSlotLabel { get; }
    public List<SessionItem> Sessions { get; }
    public int SessionCount => Sessions.Count;

    public ConflictGroup(string timeSlotLabel, List<SessionItem> sessions)
    {
        TimeSlotLabel = timeSlotLabel;
        Sessions = sessions;
    }
}
