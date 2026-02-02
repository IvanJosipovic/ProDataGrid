using System.Collections;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace ProDataGrid.ExcelSample.Controls;

public sealed class ExcelRibbonBar : TemplatedControl
{
    public static readonly DirectProperty<ExcelRibbonBar, IEnumerable?> TabsProperty =
        AvaloniaProperty.RegisterDirect<ExcelRibbonBar, IEnumerable?>(
            nameof(Tabs),
            o => o.Tabs,
            (o, v) => o.Tabs = v);

    public static readonly DirectProperty<ExcelRibbonBar, object?> SelectedTabProperty =
        AvaloniaProperty.RegisterDirect<ExcelRibbonBar, object?>(
            nameof(SelectedTab),
            o => o.SelectedTab,
            (o, v) => o.SelectedTab = v);

    public static readonly DirectProperty<ExcelRibbonBar, IEnumerable?> QuickCommandsProperty =
        AvaloniaProperty.RegisterDirect<ExcelRibbonBar, IEnumerable?>(
            nameof(QuickCommands),
            o => o.QuickCommands,
            (o, v) => o.QuickCommands = v);

    public static readonly DirectProperty<ExcelRibbonBar, string?> SearchTextProperty =
        AvaloniaProperty.RegisterDirect<ExcelRibbonBar, string?>(
            nameof(SearchText),
            o => o.SearchText,
            (o, v) => o.SearchText = v);

    private IEnumerable? _tabs;
    private object? _selectedTab;
    private IEnumerable? _quickCommands;
    private string? _searchText;

    public IEnumerable? Tabs
    {
        get => _tabs;
        set => SetAndRaise(TabsProperty, ref _tabs, value);
    }

    public object? SelectedTab
    {
        get => _selectedTab;
        set => SetAndRaise(SelectedTabProperty, ref _selectedTab, value);
    }

    public IEnumerable? QuickCommands
    {
        get => _quickCommands;
        set => SetAndRaise(QuickCommandsProperty, ref _quickCommands, value);
    }

    public string? SearchText
    {
        get => _searchText;
        set => SetAndRaise(SearchTextProperty, ref _searchText, value);
    }
}
