using Conference.Maui.ViewModels;
using Plugin.Maui.SwipeCardView.Core;

namespace Conference.Maui.Pages;

public partial class MyAgendaPage : ContentPage
{
    private readonly MyAgendaViewModel _viewModel;
    
    public MyAgendaPage(MyAgendaViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

   
}