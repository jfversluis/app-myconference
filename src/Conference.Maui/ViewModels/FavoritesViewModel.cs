using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Conference.Maui.Pages;
using Microsoft.Extensions.Logging;
using Sessionize.Api.Client.DataTransferObjects;
using Sessionize.Api.Client.ValueObjects;

namespace Conference.Maui.ViewModels;

public partial class FavoritesViewModel : BaseViewModel, IRecipient<FavoriteChangedMessage>
{
    private readonly IConferenceDataService _dataService;
    private readonly IFavoritesService _favoritesService;
    private readonly ILogger<FavoritesViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<TimeSlotGroup> _favoriteSlots = [];

    [ObservableProperty]
    private bool _hasFavorites;

    public FavoritesViewModel(
        IConferenceDataService dataService,
        IFavoritesService favoritesService,
        ILogger<FavoritesViewModel> logger)
    {
        _dataService = dataService;
        _favoritesService = favoritesService;
        _logger = logger;
        Title = "My Agenda";

        WeakReferenceMessenger.Default.Register<FavoriteChangedMessage>(this);
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            await BuildFavoriteSlotsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading favorites");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    private async Task BuildFavoriteSlotsAsync()
    {
        var allData = await _dataService.GetAllDataAsync();
        if (allData == null) return;

        var favorites = await _favoritesService.GetFavoriteSessionIdsAsync();
        var favoriteSessions = allData.Sessions
            .Where(s => favorites.Contains(s.Id))
            .OrderBy(s => s.StartsAt)
            .ToList();

        var slots = favoriteSessions
            .GroupBy(s => (s.StartsAt, s.EndsAt))
            .OrderBy(g => g.Key.StartsAt)
            .Select(slotGroup =>
            {
                var firstSession = slotGroup.First();
                var date = new DateOnly(firstSession.StartsAt.Year, firstSession.StartsAt.Month, firstSession.StartsAt.Day);
                var slot = new TimeSlotGroup
                {
                    StartTime = slotGroup.Key.StartsAt,
                    EndTime = slotGroup.Key.EndsAt,
                    Date = date
                };

                foreach (var session in slotGroup.OrderBy(s => allData.Rooms.FirstOrDefault(r => r.Id == s.RoomId)?.Name))
                {
                    slot.Add(CreateSessionItem(session, allData, favorites));
                }

                return slot;
            })
            .ToList();

        FavoriteSlots = new ObservableCollection<TimeSlotGroup>(slots);
        HasFavorites = slots.Count > 0;
    }

    private static SessionItem CreateSessionItem(SessionDetails session, AllDataResponse allData, IReadOnlySet<string> favorites)
    {
        var speakers = session.Speakers
            .Select(speakerId => allData.Speakers.FirstOrDefault(s => s.Id == speakerId))
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
            RoomName = allData.Rooms.FirstOrDefault(r => r.Id == session.RoomId)?.Name,
            Speakers = speakers,
            IsFavorite = true
        };
    }

    [RelayCommand]
    private async Task NavigateToSessionDetailsAsync(SessionItem session)
    {
        await Shell.Current.GoToAsync(nameof(SessionDetailsPage), new Dictionary<string, object>
        {
            ["SessionId"] = session.Id
        });
    }

    public void Receive(FavoriteChangedMessage message)
    {
        // Rebuild the list when favorites change
        _ = MainThread.InvokeOnMainThreadAsync(async () =>
        {
            try
            {
                await BuildFavoriteSlotsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating favorites view");
            }
        });
    }
}
