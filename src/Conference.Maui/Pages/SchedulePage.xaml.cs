using Conference.Maui.Helpers;
using Conference.Maui.ViewModels;
using Conference.Maui.Models;
using Syncfusion.Maui.Toolkit.TabView;
using System.Collections.ObjectModel;

namespace Conference.Maui.Pages;

public partial class SchedulePage : ContentPage
{
	private readonly ScheduleViewModel _viewModel;
	private AppTheme _currentTheme;

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
        
        // Initialize current theme
        _currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
        
        // Subscribe to theme changes to update tab colors
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        // Unsubscribe from theme changes
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
        }
        
        base.OnNavigatedFrom(args);
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e)
    {
        // Only update if the theme actually changed
        if (e.RequestedTheme != _currentTheme)
        {
            _currentTheme = e.RequestedTheme;
            // Update tab text colors when theme changes
            UpdateTabTextColors();
        }
    }

    private void UpdateTabTextColors()
    {
        if (tabView?.Items is null || tabView.Items.Count == 0)
        {
            return;
        }

        var textColor = GetThemeAwareTextColor();
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
                    TextColor = GetThemeAwareTextColor()
                };

                tabView.Items.Add(tabItem);
            }
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

        // Wrap CollectionView in RefreshView for pull-to-refresh
        RefreshView refreshView = new()
        {
            Content = collectionView
        };

        // Bind RefreshView to ViewModel's refresh properties
        refreshView.SetBinding(RefreshView.IsRefreshingProperty, new Binding("IsRefreshing"));
        refreshView.SetBinding(RefreshView.CommandProperty, new Binding("RefreshDataCommand"));

        return refreshView;
    }

    private Color GetThemeAwareTextColor()
    {
        // Get the appropriate color based on current theme
        var currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
        
        if (currentTheme == AppTheme.Dark)
        {
            // Try to get DarkTextPrimary from resources
            if (Application.Current?.Resources.TryGetValue("DarkTextPrimary", out var darkColor) == true && darkColor is Color darkTextColor)
            {
                return darkTextColor;
            }
            return Color.FromArgb("#F9FAFB"); // Fallback light text for dark theme
        }
        else
        {
            // Try to get LightTextPrimary from resources
            if (Application.Current?.Resources.TryGetValue("LightTextPrimary", out var lightColor) == true && lightColor is Color lightTextColor)
            {
                return lightTextColor;
            }
            return Color.FromArgb("#1A1A1A"); // Fallback dark text for light theme
        }
    }
}