using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Data;
using Lang.Avalonia.MarkupExtensions;
using Prism.Regions;

namespace Vex.Core.Regions;

public sealed class TabControlRegionAdapter : RegionAdapterBase<TabControl>
{
    public TabControlRegionAdapter(IRegionBehaviorFactory regionBehaviorFactory)
        : base(regionBehaviorFactory)
    {
    }

    protected override void Adapt(IRegion region, TabControl regionTarget)
    {
        ArgumentNullException.ThrowIfNull(region);
        ArgumentNullException.ThrowIfNull(regionTarget);

        AddViews(regionTarget, region.Views);
        region.Views.CollectionChanged += (_, e) => ApplyViewChanges(regionTarget, e);
    }

    protected override IRegion CreateRegion()
    {
        return new SingleActiveRegion();
    }

    private static void ApplyViewChanges(TabControl regionTarget, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems is not null)
        {
            AddViews(regionTarget, e.NewItems.Cast<object?>());
            return;
        }

        if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems is not null)
        {
            RemoveViews(regionTarget, e.OldItems.Cast<object?>());
        }
    }

    private static void AddViews(TabControl regionTarget, IEnumerable<object?> views)
    {
        foreach (var view in views)
        {
            if (view is null)
            {
                continue;
            }

            regionTarget.Items.Add(CreateTabItem(view));
        }
    }

    private static void RemoveViews(TabControl regionTarget, IEnumerable<object?> views)
    {
        foreach (var view in views)
        {
            var tabItem = regionTarget.Items
                .OfType<TabItem>()
                .FirstOrDefault(item => ReferenceEquals(item.Content, view));

            if (tabItem is not null)
            {
                regionTarget.Items.Remove(tabItem);
            }
        }
    }

    private static TabItem CreateTabItem(object view)
    {
        var tabItem = new TabItem { Content = view };
        var headerKey = ResolveHeaderKey(view);
        if (new I18nBinding(headerKey).ProvideValue(null!) is BindingBase headerBinding)
        {
            tabItem.Bind(TabItem.HeaderProperty, headerBinding);
        }

        return tabItem;
    }

    private static string ResolveHeaderKey(object view)
    {
        // 页签标题优先读取 View 上的附加属性，避免 Region 创建时 AutoWireViewModel 尚未完成。
        if (view is Control control)
        {
            var attachedHeaderKey = RegionTab.GetHeaderKey(control);
            if (!string.IsNullOrWhiteSpace(attachedHeaderKey))
            {
                return attachedHeaderKey;
            }
        }

        return view is Control { DataContext: IRegionTabItem tabItem } && !string.IsNullOrWhiteSpace(tabItem.TitleKey)
            ? tabItem.TitleKey
            : view.GetType().Name;
    }
}
