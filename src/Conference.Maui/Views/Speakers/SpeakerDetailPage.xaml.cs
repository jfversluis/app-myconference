using Conference.Maui.ViewModels;

namespace Conference.Maui.Views.Speakers;

public partial class SpeakerDetailPage : ContentPage
{
	public SpeakerDetailPage(SpeakerDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
