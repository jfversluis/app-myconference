using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SessionDetailsPage : ContentPage
{
	public SessionDetailsPage(SessionDetailsViewModel sessionDetailsViewModel)
	{
		InitializeComponent();

		BindingContext = sessionDetailsViewModel;
	}
}