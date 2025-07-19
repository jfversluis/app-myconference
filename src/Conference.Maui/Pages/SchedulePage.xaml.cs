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
                SfTabItem tabItem = new()
                {
                    Header = daySchedule.TabTitle,
                    Content = CreateTabContent(daySchedule.TimeSlots),
                };

                tabView.Items.Add(tabItem);
            }
        }
    }

    private View CreateTabContent(ObservableCollection<TimeSlot> timeSlots)
    {
        // Create flattened items for this tab
        var flattenedItems = new List<object>();
        
        foreach (var timeSlot in timeSlots)
        {
            // Add time header
            flattenedItems.Add(new TimeHeader 
            { 
                TimeDisplayText = timeSlot.TimeDisplayText, 
                SessionCount = timeSlot.Sessions.Count 
            });
            
            // Add all sessions for this time slot
            foreach (var session in timeSlot.Sessions)
            {
                flattenedItems.Add(session);
            }
        }

        CollectionView collectionView = new()
        {
            Margin = new Thickness(10),
            ItemsSource = flattenedItems,
            ItemTemplate = (DataTemplateSelector)Resources["ScheduleTemplateSelector"],
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
            {
                ItemSpacing = 5
            }
        };

        return collectionView;
    }
}