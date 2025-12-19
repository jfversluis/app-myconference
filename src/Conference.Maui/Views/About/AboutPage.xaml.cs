using Conference.Maui.ViewModels;

namespace Conference.Maui.Views.About;

public partial class AboutPage : ContentPage
{
	public AboutPage(AboutViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
