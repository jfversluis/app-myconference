using Conference.Maui.Pages;
#if IOS
using UIKit;
#elif ANDROID
using Google.Android.Material.Navigation;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;
#endif

namespace Conference.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(SessionDetailsPage), typeof(SessionDetailsPage));
        Routing.RegisterRoute(nameof(SpeakerDetailsPage), typeof(SpeakerDetailsPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(QuickPickPage), typeof(QuickPickPage));
        Routing.RegisterRoute(nameof(ConflictResolverPage), typeof(ConflictResolverPage));
    }

#if IOS
    private bool _tabReselectionSetup;

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        if (!_tabReselectionSetup)
        {
            _tabReselectionSetup = true;
            SetupScrollToTopOnTabReselect();
        }
    }

    private void SetupScrollToTopOnTabReselect()
    {
        if (Handler?.PlatformView is not UIView platformView)
            return;

        var tabBar = FindDescendant<UITabBar>(platformView);
        if (tabBar is null)
            return;

        var tap = new UITapGestureRecognizer(HandleTabBarTap);
        tap.ShouldRecognizeSimultaneously = (_, _) => true;
        tap.DelaysTouchesBegan = false;
        tap.DelaysTouchesEnded = false;
        tap.CancelsTouchesInView = false;
        tabBar.AddGestureRecognizer(tap);
    }

    private void HandleTabBarTap(UITapGestureRecognizer gesture)
    {
        if (gesture.View is not UITabBar tabBar)
            return;

        var items = tabBar.Items;
        if (items is null || items.Length == 0)
            return;

        var location = gesture.LocationInView(tabBar);
        var itemWidth = tabBar.Bounds.Width / items.Length;
        var tappedIndex = Math.Clamp((int)(location.X / itemWidth), 0, items.Length - 1);
        var selectedIndex = Array.IndexOf(items, tabBar.SelectedItem);

        if (tappedIndex == selectedIndex && Current?.CurrentPage is IScrollToTop scrollable)
        {
            scrollable.ScrollToTop();
        }
    }

    private static T? FindDescendant<T>(UIView view) where T : UIView
    {
        if (view is T match) return match;
        foreach (var subview in view.Subviews)
        {
            var found = FindDescendant<T>(subview);
            if (found is not null) return found;
        }
        return null;
    }
#elif ANDROID
    private bool _tabReselectionSetup;

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);

        if (!_tabReselectionSetup)
        {
            _tabReselectionSetup = true;
            SetupScrollToTopOnTabReselect();
        }
    }

    private void SetupScrollToTopOnTabReselect()
    {
        if (Handler?.PlatformView is not AView platformView)
            return;

        var navView = FindDescendant<NavigationBarView>(platformView);
        if (navView is null)
            return;

        navView.ItemReselected += OnTabReselected;
    }

    private void OnTabReselected(object? sender, NavigationBarView.ItemReselectedEventArgs e)
    {
        if (Current?.CurrentPage is IScrollToTop scrollable)
        {
            scrollable.ScrollToTop();
        }
    }

    private static T? FindDescendant<T>(AView? view) where T : AView
    {
        if (view is null) return null;
        if (view is T match) return match;
        if (view is AViewGroup vg)
        {
            for (int i = 0; i < vg.ChildCount; i++)
            {
                var found = FindDescendant<T>(vg.GetChildAt(i));
                if (found is not null) return found;
            }
        }
        return null;
    }
#endif
}
