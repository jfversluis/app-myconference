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
    private bool _hasUserScrolled;
    private nfloat _initialContentOffsetY;
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
        _hasUserScrolled = false;
        _initialContentOffsetY = native.ContentOffset.Y;

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

        // Detect user scroll: content offset moved >20pts from initial position.
        // This avoids firing on layout passes and programmatic adjustments at page load.
        if (!_hasUserScrolled)
        {
            if (Math.Abs(cv.ContentOffset.Y - _initialContentOffsetY) > 20)
                _hasUserScrolled = true;
            else
                return;
        }

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
    /// Determines which section's sticky header is visually dominant at the pinned position.
    ///
    /// During a header push transition, the outgoing header (section N) is pushed upward
    /// while the incoming header (section N+1) approaches from below. Rather than using
    /// an arbitrary pixel offset, we compute the exact push fraction:
    ///   pushFraction = (pinnedY - outgoingHeader.frame.Y) / outgoingHeader.height
    ///
    /// The transition fires at the 50% crossover — when the outgoing header has been
    /// pushed past half its height, the incoming header is visually dominant.
    /// </summary>
    private static int GetPinnedHeaderSection(UICollectionView cv)
    {
        nfloat pinnedY = cv.ContentOffset.Y + cv.AdjustedContentInset.Top;

        var searchRect = new CGRect(0, pinnedY - 80, cv.Bounds.Width, 200);

        var attributes = cv.CollectionViewLayout.LayoutAttributesForElementsInRect(searchRect);
        if (attributes is null or { Length: 0 })
            return -1;

        // Single-pass: find the header spanning pinnedY with highest section index,
        // or closest header as fallback. No allocations.
        int pinnedSection = -1;
        int nearestSection = -1;
        nfloat nearestDist = nfloat.MaxValue;
        int pinnedNextSection = -1;

        foreach (var attr in attributes)
        {
            if (attr.RepresentedElementCategory != UICollectionElementCategory.SupplementaryView)
                continue;
            if (attr.RepresentedElementKind != UICollectionElementKindSectionKey.Header)
                continue;

            int section = (int)attr.IndexPath.Section;
            var frame = attr.Frame;

            // Does this header span the pinnedY line?
            if (frame.Y <= pinnedY + 1 && frame.Y + frame.Height >= pinnedY - 1)
            {
                nfloat pushFraction = frame.Height > 0 ? (pinnedY - frame.Y) / frame.Height : 0;

                if (pushFraction > 0.5f)
                {
                    // This header is being pushed out — remember it but look for its successor
                    pinnedSection = section;
                }
                else
                {
                    // This header is the dominant one at the pin position
                    return section;
                }
            }
            else
            {
                // Track nearest header for fallback
                nfloat dist = (nfloat)Math.Min(
                    Math.Abs(frame.Y - pinnedY),
                    Math.Abs(frame.Y + frame.Height - pinnedY));
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestSection = section;
                }

                // Track if this is the successor to a pushed-out header
                if (pinnedSection >= 0 && section > pinnedSection &&
                    (pinnedNextSection < 0 || section < pinnedNextSection))
                {
                    pinnedNextSection = section;
                }
            }
        }

        // Return successor of pushed-out header, or pushed-out header itself, or nearest
        if (pinnedNextSection >= 0) return pinnedNextSection;
        if (pinnedSection >= 0) return pinnedSection;
        return nearestSection;
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
        _hasUserScrolled = false;
    }
#endif
}
