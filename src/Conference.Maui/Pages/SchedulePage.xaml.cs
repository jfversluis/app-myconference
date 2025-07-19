using Conference.Maui.Helpers;
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
        
        // Subscribe to theme changes
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged += OnThemeChanged;
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        // Unsubscribe from theme changes
        if (Application.Current != null)
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        base.OnNavigatedFrom(args);
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        // Update tab colors when theme changes
        UpdateTabColors();
    }

    private void UpdateTabColors()
    {
        if (tabView?.Items == null) return;

        Color textColor = Application.Current?.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#F9FAFB") // DarkTextPrimary
            : Color.FromArgb("#1A1A1A"); // LightTextPrimary

        foreach (SfTabItem tabItem in tabView.Items)
        {
            tabItem.TextColor = textColor;
        }
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

            // Set initial tab colors
            UpdateTabColors();
        }
    }

    private View CreateTabContent(ObservableCollection<TimeSlot> timeSlots)
    {
        // Create flattened items for this tab using the shared helper
        var flattenedItems = ScheduleHelper.FlattenTimeSlots(timeSlots);

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