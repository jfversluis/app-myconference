using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SpeakersPage : ContentPage
{
    public SpeakersPage(SpeakersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
