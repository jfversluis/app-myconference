using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SpeakerDetailsPage : ContentPage
{
	public SpeakerDetailsPage(SpeakerDetailsViewModel speakerDetailsViewModel)
	{
		InitializeComponent();

		BindingContext = speakerDetailsViewModel;
	}
}