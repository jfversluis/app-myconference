using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Microsoft.Extensions.Logging;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.ViewModels;

public partial class ConflictResolverViewModel : ObservableObject
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<ConflictResolverViewModel> _logger;
    private AllDataResponse? _allData;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _totalConflicts;

    [ObservableProperty]
    private int _resolvedCount;

    public ObservableCollection<ConflictGroup> ConflictGroups { get; } = [];

    public bool AllResolved => ConflictGroups.Count == 0 && !IsBusy;

    public ConflictResolverViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ILogger<ConflictResolverViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _logger = logger;
    }

    public async Task LoadConflictsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        try
        {
            _allData = await _dataService.GetAllDataAsync();
            if (_allData == null) return;

            var favoriteIds = await _favoritesService.GetFavoriteSessionIdsAsync();

            var favSessions = _allData.Sessions
                .Where(s => favoriteIds.Contains(s.Id))
                .OrderBy(s => s.StartsAt)
                .Select(CreateSessionItem)
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
            OnPropertyChanged(nameof(AllResolved));
        }
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

    [RelayCommand]
    private async Task KeepSessionAsync(SessionItem session)
    {
        var group = ConflictGroups.FirstOrDefault(g => g.Sessions.Contains(session));
        if (group == null) return;

        try
        {
            foreach (var other in group.Sessions.Where(s => s.Id != session.Id))
            {
                await _favoritesService.RemoveFavoriteAsync(other.Id);
                other.IsFavorite = false;
                _logger.LogDebug("Unfavorited conflicting session: {Title}", other.Title);
            }

            session.IsFavorite = true;
            ConflictGroups.Remove(group);
            ResolvedCount++;

            OnPropertyChanged(nameof(AllResolved));

            _logger.LogInformation("Kept \"{Title}\", removed {Count} conflicts",
                session.Title, group.Sessions.Count - 1);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving conflict for session {Id}", session.Id);
        }
    }

    [RelayCommand]
    private async Task CloseAsync()
    {
        await Shell.Current.GoToAsync("..");
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
