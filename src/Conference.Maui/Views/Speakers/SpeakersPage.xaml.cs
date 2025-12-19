using Conference.Maui.ViewModels;

namespace Conference.Maui.Views.Speakers;

public partial class SpeakersPage : ContentPage
{
	private readonly SpeakersViewModel _viewModel;

	public SpeakersPage(SpeakersViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeAsync();
	}
}
