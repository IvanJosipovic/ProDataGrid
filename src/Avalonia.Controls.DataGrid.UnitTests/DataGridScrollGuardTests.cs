using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests;

public class DataGridScrollGuardTests
{
    [AvaloniaFact]
    public void ScrollSlotsByHeight_Normalizes_FirstScrollingSlot_When_Negative()
    {
        var items = new List<ScrollGuardItem>
        {
            new("Item 0"),
            new("Item 1")
        };
        var target = CreateTarget(items);

        target.DisplayData.FirstScrollingSlot = -1;
        var previousNegOffset = 12.0;
        SetPrivateProperty(target, "NegVerticalOffset", previousNegOffset);

        InvokePrivate(target, "ScrollSlotsByHeight", 1.0);

        Assert.True(target.DisplayData.FirstScrollingSlot >= 0);
        Assert.True(target.NegVerticalOffset >= 0);
        Assert.True(target.NegVerticalOffset < previousNegOffset);
    }

    private static DataGrid CreateTarget(IList<ScrollGuardItem> items)
    {
        var root = new Window
        {
            Width = 300,
            Height = 200
        };

        root.SetThemeStyles();

        var target = new DataGrid
        {
            ItemsSource = items,
            HeadersVisibility = DataGridHeadersVisibility.Column
        };
        target.ColumnsInternal.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding("Name") });

        root.Content = target;
        root.Show();
        root.UpdateLayout();
        return target;
    }

    private static void InvokePrivate(object target, string name, params object[] args)
    {
        var method = target.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);

        Assert.NotNull(method);
        method!.Invoke(target, args);
    }

    private static void SetPrivateProperty(object target, string name, object value)
    {
        var property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(property);
        property!.SetValue(target, value);
    }

    private sealed class ScrollGuardItem
    {
        public ScrollGuardItem(string name) => Name = name;

        public string Name { get; }
    }
}
