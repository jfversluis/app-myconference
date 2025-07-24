using Conference.Maui.ViewModels;
using Conference.Maui.Services;
using Conference.Maui.Models;
using CommunityToolkit.Maui.Alerts;

namespace Conference.Maui.Pages;

public partial class SpeakersPage : ContentPage
{
    private readonly SpeakersViewModel _viewModel;
    private readonly RefreshService _refreshService;

    public SpeakersPage(SpeakersViewModel speakersViewModel, RefreshService refreshService)
	{
		InitializeComponent();
        _viewModel = speakersViewModel;
        _refreshService = refreshService;
        BindingContext = _viewModel;
        
        // Setup manual refresh event handler
        speakersRefreshView.Refreshing += async (sender, e) =>
        {
            if (sender is not RefreshView refView) return;

            try
            {
                // Set to refreshing
                refView.IsRefreshing = true;
                
                // Call RefreshService directly with toasts
                var result = await _refreshService.RefreshWithFeedbackAsync("Speakers");
                
                // Update UI if data was refreshed
                if (result == RefreshResult.DataUpdated)
                {
                    await _viewModel.LoadSpeakersData();
                }
            }
            catch (Exception ex)
            {
                var errorToast = Toast.Make($"Failed to refresh: {ex.Message}", CommunityToolkit.Maui.Core.ToastDuration.Long);
                await errorToast.Show();
            }
            finally
            {
                // Ensure it stops refreshing
                refView.IsRefreshing = false;
            }
        };
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        await _viewModel.InitializeAsync();
    }
}