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
               
            }
            else if (args.Direction == SwipeCardDirection.Left)
            {
                // Nope - remove from list
                // Do nothing with the database for non-favorites
            }

            // Check if we've swiped all cards
        }

        [RelayCommand]
        private void Dragging(DraggingCardEventArgs args)
        {
            // Optional animation effects during dragging
        }
    }
}
