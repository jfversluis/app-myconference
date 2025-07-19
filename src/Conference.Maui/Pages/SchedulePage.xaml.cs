using Conference.Maui.ViewModels;
using Conference.Maui.Models;
using Syncfusion.Maui.Toolkit.TabView;
using System.Collections.ObjectModel;

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
        CreateTabs();
    }

    private void CreateTabs()
    {
        if (tabView is null)
        {
            return;
        }
        
        if (_viewModel.ShowTabs)
        {
            tabView.Items.Clear();

            foreach (var daySchedule in _viewModel.ScheduleDays)
            {
                var tabItem = new SfTabItem
                {
                    Header = daySchedule.TabTitle,
                    Content = CreateTabContent(daySchedule.Sessions)
                };

                tabView.Items.Add(tabItem);
            }
        }
    }

    private View CreateTabContent(ObservableCollection<Session> sessions)
    {
        CollectionView collectionView = new()
        {
            Margin = new Thickness(10),
            ItemsSource = sessions,
            ItemTemplate = (DataTemplate)Resources["SessionTemplate"],
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
            {
                ItemSpacing = 5
            }
        };

        return collectionView;
    }
}