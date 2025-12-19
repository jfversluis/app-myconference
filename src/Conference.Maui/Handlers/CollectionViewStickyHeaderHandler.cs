using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Platform;

#if IOS
using UIKit;
using Foundation;
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
                System.Diagnostics.Debug.WriteLine($"🔧 CollectionView found: {collectionView}");
                System.Diagnostics.Debug.WriteLine($"🔧 Layout type: {collectionView.CollectionViewLayout?.GetType().Name}");
                
                if (collectionView.CollectionViewLayout is UICollectionViewFlowLayout flowLayout)
                {
                    System.Diagnostics.Debug.WriteLine("✅ Setting SectionHeadersPinToVisibleBounds = true");
                    flowLayout.SectionHeadersPinToVisibleBounds = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ Layout is not UICollectionViewFlowLayout, trying to replace it");
                    
                    // The layout might be a custom one, let's create a new FlowLayout
                    var newLayout = new UICollectionViewFlowLayout
                    {
                        SectionHeadersPinToVisibleBounds = true,
                        EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize,
                        ScrollDirection = UICollectionViewScrollDirection.Vertical
                    };
                    
                    collectionView.SetCollectionViewLayout(newLayout, false);
                    System.Diagnostics.Debug.WriteLine("✅ Replaced with UICollectionViewFlowLayout with sticky headers");
                }
            }
        });
#endif
    }
}
