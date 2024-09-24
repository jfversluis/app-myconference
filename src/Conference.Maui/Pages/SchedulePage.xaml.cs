using Conference.Maui.ViewModels;

namespace Conference.Maui.Pages;

public partial class SchedulePage : ContentPage
{
	private readonly ScheduleViewModel _viewModel;

	public SchedulePage(ScheduleViewModel scheduleViewModel)
	{
		InitializeComponent();

        _viewModel = scheduleViewModel;
        BindingContext = _viewModel;
	}

    protected override async void OnNavigatedTo(NavigatedToEventArgs args)
    {
        await _viewModel.LoadEventData();
    }
}