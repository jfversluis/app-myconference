using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Conference.Maui.Interfaces;
using Conference.Maui.Models;
using Plugin.Maui.SwipeCardView.Core;
using System.Collections.ObjectModel;

namespace Conference.Maui.ViewModels
{
    public partial class PickFavoriteSessionsViewModel : ObservableObject, IQueryAttributable
    {
        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.ContainsKey("AllSessions") && query["AllSessions"] is List<Session> sessions)
            {
                foreach (var session in sessions)
                {
                    Sessions.Add(session);
                }
            }
        }

        readonly IDatabaseService _databaseService;

        public ObservableCollection<Session> Sessions { get; set; } = [];

        [ObservableProperty]
        private bool _hasSwipedAllCards;

        [ObservableProperty]
        private double _threshold = 100;

        public PickFavoriteSessionsViewModel(IDatabaseService databaseService) 
        {
            _databaseService = databaseService;
        }

        [RelayCommand]
        private async Task Swiped(SwipedCardEventArgs args)
        {
            if (args.Direction == SwipeCardDirection.Right)
            {
                // Like - add to favorites
                // We need to get the current session from the top of the deck
                if (Sessions.Count > 0)
                {
                    var session = Sessions.First();
                    var favoriteSession = new FavoriteSession
                    {
                        SessionId = session.Id,
                        IsFavorite = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _databaseService.SaveFavoriteSessionAsync(favoriteSession);
                    session.IsFavorite = true;
                }
            }
            else if (args.Direction == SwipeCardDirection.Left)
            {
                // Nope - just remove from consideration, don't mark as favorite
                if (Sessions.Count > 0)
                {
                    var session = Sessions.First();
                    // Ensure it's not marked as favorite
                    await _databaseService.DeleteFavoriteSessionAsync(session.Id);
                    session.IsFavorite = false;
                }
            }

            // Check if we've swiped all cards
            HasSwipedAllCards = Sessions.Count <= 1; // Account for the current card being swiped
        }

        [RelayCommand]
        private void Dragging(DraggingCardEventArgs args)
        {
            // Optional animation effects during dragging
        }
    }
}
