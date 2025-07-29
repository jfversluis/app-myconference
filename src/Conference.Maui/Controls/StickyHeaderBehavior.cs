using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace Conference.Maui.Controls;

public class StickyHeaderBehavior : Behavior<CollectionView>
{
    public static readonly BindableProperty StickyHeaderTemplateProperty =
        BindableProperty.Create(nameof(StickyHeaderTemplate), typeof(DataTemplate), typeof(StickyHeaderBehavior));

    public static readonly BindableProperty HeaderContainerProperty =
        BindableProperty.Create(nameof(HeaderContainer), typeof(ContentView), typeof(StickyHeaderBehavior));

    public DataTemplate? StickyHeaderTemplate
    {
        get => (DataTemplate?)GetValue(StickyHeaderTemplateProperty);
        set => SetValue(StickyHeaderTemplateProperty, value);
    }

    public ContentView? HeaderContainer
    {
        get => (ContentView?)GetValue(HeaderContainerProperty);
        set => SetValue(HeaderContainerProperty, value);
    }

    private CollectionView? _associatedObject;
    private object? _currentStickyHeader;
    private List<object>? _cachedItems; // Cache the items list
    private bool _lastShouldShowSticky = false; // Track last state to avoid redundant updates
    private bool _isAnimating = false; // Prevent overlapping animations

    protected override void OnAttachedTo(CollectionView bindable)
    {
        _associatedObject = bindable;
        bindable.Scrolled += OnScrolled;
        
        // Cache items when attached and when items source changes
        if (bindable.ItemsSource != null)
        {
            _cachedItems = bindable.ItemsSource.Cast<object>().ToList();
        }
        
        // Listen for ItemsSource changes to update cache
        bindable.PropertyChanged += OnCollectionViewPropertyChanged;
        
        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
        if (_associatedObject != null)
        {
            _associatedObject.Scrolled -= OnScrolled;
            _associatedObject.PropertyChanged -= OnCollectionViewPropertyChanged;
        }
        _associatedObject = null;
        _cachedItems = null;
        base.OnDetachingFrom(bindable);
    }

    private void OnCollectionViewPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CollectionView.ItemsSource))
        {
            var collectionView = sender as CollectionView;
            if (collectionView?.ItemsSource != null)
            {
                _cachedItems = collectionView.ItemsSource.Cast<object>().ToList();
            }
            else
            {
                _cachedItems = null;
            }
        }
    }

    private void OnScrolled(object? sender, ItemsViewScrolledEventArgs e)
    {
        if (_associatedObject?.ItemsSource == null || HeaderContainer == null || StickyHeaderTemplate == null || _cachedItems == null)
            return;

        // Find the current header based on scroll position
        object? currentHeader = null;
        bool shouldShowSticky = true;
        
        // Use FirstVisibleItemIndex to determine which header should be sticky
        int visibleIndex = (int)e.FirstVisibleItemIndex;
        
        // Early exit if we're beyond the list
        if (visibleIndex >= _cachedItems.Count)
            return;
        
        // Walk backwards from the current position to find the most recent header
        for (int i = visibleIndex; i >= 0; i--)
        {
            if (i < _cachedItems.Count && IsHeaderItem(_cachedItems[i]))
            {
                currentHeader = _cachedItems[i];
                
                // If the header we found is the first visible item, don't show sticky
                // This means the original header is still visible
                if (i == visibleIndex && e.VerticalOffset <= 10) // Small threshold for scroll position
                {
                    shouldShowSticky = false;
                }
                break;
            }
        }

        // Hide sticky header if we're at the very top of the list
        if (e.VerticalOffset <= 0)
        {
            shouldShowSticky = false;
        }

        // Only update if the state actually changed to avoid unnecessary work
        var newStickyHeader = shouldShowSticky ? currentHeader : null;
        if (newStickyHeader != _currentStickyHeader || shouldShowSticky != _lastShouldShowSticky)
        {
            var oldHeader = _currentStickyHeader;
            _currentStickyHeader = newStickyHeader;
            _lastShouldShowSticky = shouldShowSticky;
            
            // Use animated update if both old and new headers exist (transition)
            if (oldHeader != null && newStickyHeader != null && oldHeader != newStickyHeader)
            {
                _ = UpdateStickyHeaderWithAnimation(oldHeader, newStickyHeader);
            }
            // Use fade animation when showing/hiding headers
            else if (oldHeader != null && newStickyHeader == null)
            {
                _ = FadeOutStickyHeader();
            }
            else if (oldHeader == null && newStickyHeader != null)
            {
                _ = FadeInStickyHeader();
            }
            else
            {
                UpdateStickyHeader();
            }
        }
    }

    private void UpdateStickyHeader()
    {
        if (HeaderContainer == null || StickyHeaderTemplate == null)
            return;

        if (_currentStickyHeader != null)
        {
            var headerContent = StickyHeaderTemplate.CreateContent() as View;
            if (headerContent != null)
            {
                headerContent.BindingContext = _currentStickyHeader;
                HeaderContainer.Content = headerContent;
                HeaderContainer.IsVisible = true;
                
                // Reset any transforms from animations
                headerContent.TranslationY = 0;
                headerContent.Opacity = 1;
            }
        }
        else
        {
            HeaderContainer.Content = null;
            HeaderContainer.IsVisible = false;
        }
    }

    private async Task UpdateStickyHeaderWithAnimation(object oldHeader, object newHeader)
    {
        if (HeaderContainer == null || StickyHeaderTemplate == null || _isAnimating)
            return;

        _isAnimating = true;

        try
        {
            var currentContent = HeaderContainer.Content as View;
            
            // Create the new header content
            var newHeaderContent = StickyHeaderTemplate.CreateContent() as View;
            if (newHeaderContent == null)
            {
                UpdateStickyHeader();
                return;
            }

            newHeaderContent.BindingContext = newHeader;
            
            // Set up initial state for new header (positioned below, hidden)
            newHeaderContent.TranslationY = HeaderContainer.Height > 0 ? HeaderContainer.Height : 50;
            newHeaderContent.Opacity = 0;

            // If we have existing content, animate it out
            if (currentContent != null)
            {
                // Start the slide-out animation for the old header
                var slideOutTask = currentContent.TranslateTo(0, -(HeaderContainer.Height > 0 ? HeaderContainer.Height : 50), 200, Easing.CubicInOut);
                var fadeOutTask = currentContent.FadeTo(0, 150, Easing.CubicInOut);

                // Set the new content while the old one animates out
                HeaderContainer.Content = newHeaderContent;
                HeaderContainer.IsVisible = true;

                // Start the slide-in animation for the new header (slight delay for better effect)
                await Task.Delay(50);
                var slideInTask = newHeaderContent.TranslateTo(0, 0, 250, Easing.CubicOut);
                var fadeInTask = newHeaderContent.FadeTo(1, 200, Easing.CubicOut);

                // Wait for all animations to complete
                await Task.WhenAll(slideOutTask, fadeOutTask, slideInTask, fadeInTask);
            }
            else
            {
                // No existing content, just slide in the new header
                HeaderContainer.Content = newHeaderContent;
                HeaderContainer.IsVisible = true;
                
                await Task.WhenAll(
                    newHeaderContent.TranslateTo(0, 0, 300, Easing.CubicOut),
                    newHeaderContent.FadeTo(1, 250, Easing.CubicOut)
                );
            }
        }
        catch (Exception)
        {
            // Fallback to non-animated update if animation fails
            UpdateStickyHeader();
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private async Task FadeInStickyHeader()
    {
        if (HeaderContainer == null || StickyHeaderTemplate == null || _currentStickyHeader == null || _isAnimating)
            return;

        _isAnimating = true;

        try
        {
            var headerContent = StickyHeaderTemplate.CreateContent() as View;
            if (headerContent == null)
            {
                UpdateStickyHeader();
                return;
            }

            headerContent.BindingContext = _currentStickyHeader;
            headerContent.Opacity = 0;
            headerContent.TranslationY = -20; // Start slightly above

            HeaderContainer.Content = headerContent;
            HeaderContainer.IsVisible = true;

            // Fade in with a subtle slide down
            await Task.WhenAll(
                headerContent.FadeTo(1, 300, Easing.CubicOut),
                headerContent.TranslateTo(0, 0, 300, Easing.CubicOut)
            );
        }
        catch (Exception)
        {
            UpdateStickyHeader();
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private async Task FadeOutStickyHeader()
    {
        if (HeaderContainer == null || _isAnimating)
            return;

        _isAnimating = true;

        try
        {
            var currentContent = HeaderContainer.Content as View;
            if (currentContent != null)
            {
                // Fade out with a subtle slide up
                await Task.WhenAll(
                    currentContent.FadeTo(0, 250, Easing.CubicIn),
                    currentContent.TranslateTo(0, -20, 250, Easing.CubicIn)
                );
            }

            HeaderContainer.Content = null;
            HeaderContainer.IsVisible = false;
        }
        catch (Exception)
        {
            HeaderContainer.Content = null;
            HeaderContainer.IsVisible = false;
        }
        finally
        {
            _isAnimating = false;
        }
    }

    private bool IsHeaderItem(object item)
    {
        return item.GetType().Name == "TimeHeader";
    }
}
