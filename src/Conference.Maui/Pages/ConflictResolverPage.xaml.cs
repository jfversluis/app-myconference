using Conference.Maui.Models;
using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class ConflictResolverPage : ContentPage
{
    private readonly ConflictResolverViewModel _viewModel;

    public ConflictResolverPage(ConflictResolverViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadConflictsAsync();
    }

    private async void OnKeepButtonClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is SessionItem session)
            await _viewModel.KeepSessionCommand.ExecuteAsync(session);
    }

    private async void OnSessionTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is SessionItem session)
        {
            await Shell.Current.GoToAsync(nameof(SessionDetailsPage), new Dictionary<string, object>
            {
                { "Session", session }
            });
        }
    }

    private async void OnViewAgendaClicked(object? sender, EventArgs e)
    {
        await _viewModel.ViewAgendaCommand.ExecuteAsync(null);
    }

    private async void OnUndoClicked(object? sender, EventArgs e)
    {
        await _viewModel.UndoCommand.ExecuteAsync(null);
    }
}
