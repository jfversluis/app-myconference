using Conference.Maui.ViewModels;

namespace Conference.Maui.Views.Sessions;

public partial class SessionDetailPage : ContentPage
{
	public SessionDetailPage(SessionDetailViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
