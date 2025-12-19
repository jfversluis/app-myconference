using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Platform;

#if IOS
using UIKit;
#endif

namespace Conference.Maui.Handlers;

public static class CollectionViewStickyHeaderHandler
{
    public static void Map()
    {
#if IOS
        Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("StickyGroupHeaders", (handler, view) =>
        {
            if (handler.PlatformView is UICollectionView collectionView)
            {
                if (collectionView.CollectionViewLayout is UICollectionViewFlowLayout flowLayout)
                {
                    flowLayout.SectionHeadersPinToVisibleBounds = true;
                }
            }
        });
#endif
    }
}
