using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SessionDetailsPage : ContentPage
{
    public SessionDetailsPage(SessionDetailsViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
