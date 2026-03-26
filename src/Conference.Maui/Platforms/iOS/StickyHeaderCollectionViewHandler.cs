using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Items2;
using UIKit;

namespace Conference.Maui.Platforms.iOS;

/// <summary>
/// Custom CollectionView handler that enables native iOS sticky (pinned) group headers
/// for grouped CollectionViews.
///
/// MAUI's CollectionViewHandler2 creates a UICollectionViewCompositionalLayout but doesn't
/// set PinToVisibleBounds on group header supplementary items. Since the internal
/// CustomUICollectionViewCompositionalLayout and its section provider are inaccessible,
/// we create a replacement layout with sticky headers enabled.
/// See: https://github.com/dotnet/maui/issues/30756
///
/// Note: Status bar tap-to-scroll-to-top (dotnet/maui#19866) is not addressed here.
/// Testing showed it doesn't work in MAUI Shell even with scrollsToTop correctly set,
/// likely due to Shell's view hierarchy having multiple competing UIScrollViews.
/// A proper fix requires changes in MAUI's Shell implementation.
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
