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
            _currentStickyHeader = newStickyHeader;
            _lastShouldShowSticky = shouldShowSticky;
            UpdateStickyHeader();
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
            }
        }
        else
        {
            HeaderContainer.Content = null;
            HeaderContainer.IsVisible = false;
        }
    }

    private bool IsHeaderItem(object item)
    {
        return item.GetType().Name == "TimeHeader";
    }
}
