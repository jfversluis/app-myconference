using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SpeakersPage : ContentPage
{
    private readonly SpeakersViewModel _viewModel;

    public SpeakersPage(SpeakersViewModel speakersViewModel)
	{
		InitializeComponent();
        _viewModel = speakersViewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        await _viewModel.LoadSpeakersData();
    }
}