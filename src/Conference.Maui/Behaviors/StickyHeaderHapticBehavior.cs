using Conference.Maui.Services;

#if IOS
using CoreGraphics;
using Foundation;
using UIKit;
#endif

namespace Conference.Maui.Behaviors;

/// <summary>
/// Provides frame-accurate haptic feedback when sticky headers transition between sections.
/// Uses native iOS KVO on contentOffset for immediate scroll callbacks (no MAUI throttle).
/// On non-iOS platforms this behavior is a no-op.
/// </summary>
public sealed class StickyHeaderHapticBehavior : Behavior<CollectionView>
{
#if IOS
    private CollectionView? _collectionView;
    private UICollectionView? _nativeCollectionView;
    private IDisposable? _contentOffsetObserver;
    private UISelectionFeedbackGenerator? _feedbackGenerator;
    private int _lastPinnedSection = -1;
#endif

    protected override void OnAttachedTo(CollectionView bindable)
    {
        base.OnAttachedTo(bindable);
#if IOS
        _collectionView = bindable;
        bindable.HandlerChanged += OnHandlerChanged;
        bindable.HandlerChanging += OnHandlerChanging;
        TryAttachNative(bindable);
#endif
    }

    protected override void OnDetachingFrom(CollectionView bindable)
    {
#if IOS
        bindable.HandlerChanged -= OnHandlerChanged;
        bindable.HandlerChanging -= OnHandlerChanging;
        DetachNative();
        _collectionView = null;
#endif
        base.OnDetachingFrom(bindable);
    }

#if IOS
    private void OnHandlerChanged(object? sender, EventArgs e)
    {
        if (sender is CollectionView cv)
            TryAttachNative(cv);
    }

    private void OnHandlerChanging(object? sender, HandlerChangingEventArgs e)
    {
        DetachNative();
    }

    private void TryAttachNative(CollectionView collectionView)
    {
        if (collectionView.Handler?.PlatformView is not UIView platformView)
            return;

        var native = FindDescendant<UICollectionView>(platformView);
        if (native is null || ReferenceEquals(native, _nativeCollectionView))
            return;

        DetachNative();

        _nativeCollectionView = native;
        _feedbackGenerator = new UISelectionFeedbackGenerator();
        _feedbackGenerator.Prepare();
        _lastPinnedSection = GetPinnedHeaderSection(native);

        // KVO on contentOffset fires every scroll frame (~60fps) — no MAUI throttle
        _contentOffsetObserver = native.AddObserver(
            "contentOffset",
            NSKeyValueObservingOptions.New,
            _ => OnNativeScrolled());
    }

    private void OnNativeScrolled()
    {
        if (_nativeCollectionView is not { } cv)
            return;

        int section = GetPinnedHeaderSection(cv);
        if (section < 0 || section == _lastPinnedSection)
            return;

        _lastPinnedSection = section;

        if (!HapticService.IsEnabled)
            return;

        _feedbackGenerator?.SelectionChanged();
        _feedbackGenerator?.Prepare();
    }

    /// <summary>
    /// Finds which section's sticky header is currently pinned at the top of the scroll area
    /// by probing layout attributes in a band around the top edge.
    /// </summary>
    private static int GetPinnedHeaderSection(UICollectionView cv)
    {
        nfloat probeY = cv.ContentOffset.Y + cv.AdjustedContentInset.Top + 1;

        // Search an 160pt band around the top to catch headers during transitions
        var searchRect = new CGRect(0, probeY - 80, cv.Bounds.Width, 160);

        var attributes = cv.CollectionViewLayout.LayoutAttributesForElementsInRect(searchRect);
        if (attributes is null or { Length: 0 })
            return -1;

        UICollectionViewLayoutAttributes? best = null;
        nfloat bestDistance = nfloat.MaxValue;

        foreach (var attr in attributes)
        {
            if (attr.RepresentedElementCategory != UICollectionElementCategory.SupplementaryView)
                continue;
            if (attr.RepresentedElementKind != UICollectionElementKindSectionKey.Header)
                continue;

            var frame = attr.Frame;

            // Header currently spans the probe line — it's pinned
            if (frame.Y <= probeY && frame.Y + frame.Height >= probeY)
            {
                // Prefer higher section index when multiple headers are at the probe
                if (best is null || attr.IndexPath.Section > best.IndexPath.Section)
                    best = attr;
                continue;
            }

            // Fallback: nearest header to the probe line
            var distance = (nfloat)Math.Min(
                Math.Abs(frame.Y - probeY),
                Math.Abs(frame.Y + frame.Height - probeY));

            if (best is null || distance < bestDistance)
            {
                best = attr;
                bestDistance = distance;
            }
        }

        return best is null ? -1 : (int)best.IndexPath.Section;
    }

    private static T? FindDescendant<T>(UIView? root) where T : UIView
    {
        if (root is null) return null;
        if (root is T match) return match;
        foreach (var child in root.Subviews)
        {
            var found = FindDescendant<T>(child);
            if (found is not null) return found;
        }
        return null;
    }

    private void DetachNative()
    {
        _contentOffsetObserver?.Dispose();
        _contentOffsetObserver = null;
        _nativeCollectionView = null;
        _feedbackGenerator = null;
        _lastPinnedSection = -1;
    }
#endif
}
