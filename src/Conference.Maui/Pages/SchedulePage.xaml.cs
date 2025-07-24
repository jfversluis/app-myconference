using Conference.Maui.ViewModels;
using Syncfusion.Maui.Toolkit.TabView;

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
        base.OnNavigatedTo(args);

        // Initialize data if needed (but don't recreate UI unnecessarily)
        await _viewModel.InitializeAsync();
        
        // Only create tabs if they truly don't exist or if we absolutely need to recreate them
        if (ShouldCreateOrRecreateTabs())
        {
            CreateTabs();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Do theme setup in OnAppearing instead of OnNavigatedTo
        // This happens after navigation completes
        UpdateThemeSubscription();
    }

    private void UpdateThemeSubscription()
    {
        // Full theme setup for slower paths
        _currentTheme = Application.Current?.RequestedTheme ?? AppTheme.Light;
        
        // Subscribe to theme changes if not already subscribed
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeChanged -= OnThemeChanged;
            Application.Current.RequestedThemeChanged += OnThemeChanged;
        }
    }

    private bool ShouldCreateOrRecreateTabs()
    {
        // Don't create tabs if we don't have data
        if (!_viewModel.HasData)
            return false;
            
        // Don't create tabs if we're not supposed to show tabs
        if (!_viewModel.ShowTabs)
            return false;
            
        // Create tabs if they don't exist at all
        if (tabView?.Items == null || tabView.Items.Count == 0)
            return true;
            
        // Only recreate if the number of days has actually changed
        if (tabView.Items.Count != _viewModel.ScheduleDays.Count)
            return true;
            
        // Otherwise, keep existing tabs to preserve scroll position
        return false;
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
                    Content = CreateTabContent(daySchedule.FlattenedItems),
                    TextColor = GetThemeAwareTextColor()
                };

                tabView.Items.Add(tabItem);
            }
        }
    }

    private View CreateTabContent(List<object> flattenedItems)
    {
        // Items are already pre-computed, just create the CollectionView
        CollectionView collectionView = new()
        {
            ItemsSource = flattenedItems,
            ItemTemplate = (DataTemplateSelector)Resources["ScheduleTemplateSelector"],
            ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical)
            {
                ItemSpacing = 5
            },
            // Add header and footer spacing like the single day view
            Header = new BoxView { HeightRequest = 8, BackgroundColor = Colors.Transparent },
            Footer = new BoxView { HeightRequest = 80, BackgroundColor = Colors.Transparent }
        };

        // Wrap the CollectionView in a RefreshView for pull-to-refresh functionality
        RefreshView refreshView = new()
        {
            Content = collectionView,
            BindingContext = _viewModel // Explicitly set the binding context
        };

        // Use event handler for manual control of refresh state
        refreshView.Refreshing += async (sender, e) =>
        {
            if (sender is not RefreshView refView) return;

            try
            {
                // Set to refreshing
                refView.IsRefreshing = true;
                
                // Execute the refresh command
                await _viewModel.RefreshCommand.ExecuteAsync(null);
            }
            catch (Exception)
            {
                // Silent handling - error message will be shown via ViewModel's ErrorMessage property
            }
            finally
            {
                // Ensure it stops refreshing
                refView.IsRefreshing = false;
            }
        };

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