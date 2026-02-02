using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using ProCharts;
using ProCharts.Skia;

namespace ProDataGrid.ExcelSample.Controls;

public sealed class ExcelChartPanel : TemplatedControl
{
    public static readonly DirectProperty<ExcelChartPanel, ChartModel?> ChartModelProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, ChartModel?>(
            nameof(ChartModel),
            o => o.ChartModel,
            (o, v) => o.ChartModel = v);

    public static readonly DirectProperty<ExcelChartPanel, SkiaChartStyle?> ChartStyleProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, SkiaChartStyle?>(
            nameof(ChartStyle),
            o => o.ChartStyle,
            (o, v) => o.ChartStyle = v);

    public static readonly DirectProperty<ExcelChartPanel, IEnumerable?> ChartTypesProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, IEnumerable?>(
            nameof(ChartTypes),
            o => o.ChartTypes,
            (o, v) => o.ChartTypes = v);

    public static readonly DirectProperty<ExcelChartPanel, object?> SelectedChartTypeProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, object?>(
            nameof(SelectedChartType),
            o => o.SelectedChartType,
            (o, v) => o.SelectedChartType = v);

    public static readonly DirectProperty<ExcelChartPanel, string?> RangeTextProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, string?>(
            nameof(RangeText),
            o => o.RangeText,
            (o, v) => o.RangeText = v);

    public static readonly DirectProperty<ExcelChartPanel, string?> StatusTextProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, string?>(
            nameof(StatusText),
            o => o.StatusText,
            (o, v) => o.StatusText = v);

    public static readonly DirectProperty<ExcelChartPanel, bool> AutoApplySelectionProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, bool>(
            nameof(AutoApplySelection),
            o => o.AutoApplySelection,
            (o, v) => o.AutoApplySelection = v);

    public static readonly DirectProperty<ExcelChartPanel, bool> HasChartProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, bool>(
            nameof(HasChart),
            o => o.HasChart,
            (o, v) => o.HasChart = v);

    public static readonly DirectProperty<ExcelChartPanel, bool> ShowPlaceholderProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, bool>(
            nameof(ShowPlaceholder),
            o => o.ShowPlaceholder,
            (o, v) => o.ShowPlaceholder = v);

    public static readonly DirectProperty<ExcelChartPanel, ICommand?> ApplySelectionCommandProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, ICommand?>(
            nameof(ApplySelectionCommand),
            o => o.ApplySelectionCommand,
            (o, v) => o.ApplySelectionCommand = v);

    public static readonly DirectProperty<ExcelChartPanel, ICommand?> ClearChartCommandProperty =
        AvaloniaProperty.RegisterDirect<ExcelChartPanel, ICommand?>(
            nameof(ClearChartCommand),
            o => o.ClearChartCommand,
            (o, v) => o.ClearChartCommand = v);

    private ChartModel? _chartModel;
    private SkiaChartStyle? _chartStyle;
    private IEnumerable? _chartTypes;
    private object? _selectedChartType;
    private string? _rangeText;
    private string? _statusText;
    private bool _autoApplySelection;
    private bool _hasChart;
    private bool _showPlaceholder;
    private ICommand? _applySelectionCommand;
    private ICommand? _clearChartCommand;

    public ChartModel? ChartModel
    {
        get => _chartModel;
        set => SetAndRaise(ChartModelProperty, ref _chartModel, value);
    }

    public SkiaChartStyle? ChartStyle
    {
        get => _chartStyle;
        set => SetAndRaise(ChartStyleProperty, ref _chartStyle, value);
    }

    public IEnumerable? ChartTypes
    {
        get => _chartTypes;
        set => SetAndRaise(ChartTypesProperty, ref _chartTypes, value);
    }

    public object? SelectedChartType
    {
        get => _selectedChartType;
        set => SetAndRaise(SelectedChartTypeProperty, ref _selectedChartType, value);
    }

    public string? RangeText
    {
        get => _rangeText;
        set => SetAndRaise(RangeTextProperty, ref _rangeText, value);
    }

    public string? StatusText
    {
        get => _statusText;
        set => SetAndRaise(StatusTextProperty, ref _statusText, value);
    }

    public bool AutoApplySelection
    {
        get => _autoApplySelection;
        set => SetAndRaise(AutoApplySelectionProperty, ref _autoApplySelection, value);
    }

    public bool HasChart
    {
        get => _hasChart;
        set => SetAndRaise(HasChartProperty, ref _hasChart, value);
    }

    public bool ShowPlaceholder
    {
        get => _showPlaceholder;
        set => SetAndRaise(ShowPlaceholderProperty, ref _showPlaceholder, value);
    }

    public ICommand? ApplySelectionCommand
    {
        get => _applySelectionCommand;
        set => SetAndRaise(ApplySelectionCommandProperty, ref _applySelectionCommand, value);
    }

    public ICommand? ClearChartCommand
    {
        get => _clearChartCommand;
        set => SetAndRaise(ClearChartCommandProperty, ref _clearChartCommand, value);
    }
}
