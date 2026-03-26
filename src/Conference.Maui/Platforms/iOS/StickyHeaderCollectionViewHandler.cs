using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using UIKit;

namespace Conference.Maui.Platforms.iOS;

/// <summary>
/// Custom CollectionView handler that enables:
/// 1. Native iOS sticky (pinned) group headers for grouped CollectionViews
/// 2. Status bar tap-to-scroll-to-top behavior
///
/// Sticky headers: MAUI's CollectionViewHandler2 creates a UICollectionViewCompositionalLayout
/// but doesn't set PinToVisibleBounds on group header supplementary items.
/// Since the internal CustomUICollectionViewCompositionalLayout and its section provider
/// are inaccessible, we create a replacement layout with sticky headers enabled.
/// See: https://github.com/dotnet/maui/issues/30756
///
/// ScrollsToTop: MAUI doesn't set UICollectionView.ScrollsToTop = true.
/// See: https://github.com/dotnet/maui/issues/19866
/// </summary>
public class StickyHeaderCollectionViewHandler : CollectionViewHandler2
{
    protected override UICollectionViewLayout SelectLayout()
    {
        var originalLayout = base.SelectLayout();

        if (originalLayout is not UICollectionViewCompositionalLayout compositionalLayout)
            return originalLayout;

        if (VirtualView is not GroupableItemsView { IsGrouped: true })
            return originalLayout;

        var originalConfig = compositionalLayout.Configuration;
        var scrollDirection = originalConfig?.ScrollDirection ?? UICollectionViewScrollDirection.Vertical;

        var config = new UICollectionViewCompositionalLayoutConfiguration
        {
            ScrollDirection = scrollDirection
        };

        if (originalConfig?.BoundarySupplementaryItems is { Length: > 0 } globalItems)
        {
            config.BoundarySupplementaryItems = globalItems;
        }

        bool hasHeader = VirtualView is GroupableItemsView giv && giv.GroupHeaderTemplate != null;
        bool hasFooter = VirtualView is GroupableItemsView givf && givf.GroupFooterTemplate != null;

        // Pre-allocate supplementary items outside the lambda to avoid per-section allocations
        var boundaryItems = BuildBoundarySupplementaryItems(hasHeader, hasFooter);

        var stickyLayout = new UICollectionViewCompositionalLayout((sectionIndex, environment) =>
        {
            var itemSize = NSCollectionLayoutSize.Create(
                NSCollectionLayoutDimension.CreateFractionalWidth(1.0f),
                NSCollectionLayoutDimension.CreateEstimated(44));
            var item = NSCollectionLayoutItem.Create(layoutSize: itemSize);

            var groupSize = NSCollectionLayoutSize.Create(
                NSCollectionLayoutDimension.CreateFractionalWidth(1.0f),
                NSCollectionLayoutDimension.CreateEstimated(44));
            var group = NSCollectionLayoutGroup.CreateHorizontal(groupSize, item, 1);

            var section = NSCollectionLayoutSection.Create(group: group);
            section.BoundarySupplementaryItems = boundaryItems;

            return section;
        }, config);

        return stickyLayout;
    }

    protected override void ConnectHandler(UIView platformView)
    {
        base.ConnectHandler(platformView);

        // Only set ScrollsToTop on grouped CollectionViews (primary page content).
        // iOS ignores status bar tap if multiple UIScrollViews have scrollsToTop=true,
        // so we avoid setting it on nested/non-grouped CollectionViews (e.g. in MyEvent).
        if (Controller?.CollectionView is not null &&
            VirtualView is GroupableItemsView { IsGrouped: true })
        {
            Controller.CollectionView.ScrollsToTop = true;
        }
    }

    protected override void DisconnectHandler(UIView platformView)
    {
        if (Controller?.CollectionView is not null)
        {
            Controller.CollectionView.ScrollsToTop = false;
        }

        base.DisconnectHandler(platformView);
    }

    private static NSCollectionLayoutBoundarySupplementaryItem[] BuildBoundarySupplementaryItems(
        bool hasHeader, bool hasFooter)
    {
        if (!hasHeader && !hasFooter)
            return [];

        var items = new List<NSCollectionLayoutBoundarySupplementaryItem>(2);

        if (hasHeader)
        {
            var headerSize = NSCollectionLayoutSize.Create(
                NSCollectionLayoutDimension.CreateFractionalWidth(1.0f),
                NSCollectionLayoutDimension.CreateEstimated(44));
            var header = NSCollectionLayoutBoundarySupplementaryItem.Create(
                headerSize,
                UICollectionElementKindSectionKey.Header,
                NSRectAlignment.Top);
            header.PinToVisibleBounds = true;
            items.Add(header);
        }

        if (hasFooter)
        {
            var footerSize = NSCollectionLayoutSize.Create(
                NSCollectionLayoutDimension.CreateFractionalWidth(1.0f),
                NSCollectionLayoutDimension.CreateEstimated(44));
            var footer = NSCollectionLayoutBoundarySupplementaryItem.Create(
                footerSize,
                UICollectionElementKindSectionKey.Footer,
                NSRectAlignment.Bottom);
            items.Add(footer);
        }

        return [.. items];
    }
}
