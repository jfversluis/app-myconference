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

    private async void OnKeepSessionTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject bo && bo.BindingContext is SessionItem session)
            await _viewModel.KeepSessionCommand.ExecuteAsync(session);
    }
}
