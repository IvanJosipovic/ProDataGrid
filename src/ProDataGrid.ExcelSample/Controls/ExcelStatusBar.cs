using Avalonia;
using Avalonia.Controls.Primitives;

namespace ProDataGrid.ExcelSample.Controls;

public sealed class ExcelStatusBar : TemplatedControl
{
    public static readonly DirectProperty<ExcelStatusBar, object?> LeftContentProperty =
        AvaloniaProperty.RegisterDirect<ExcelStatusBar, object?>(
            nameof(LeftContent),
            o => o.LeftContent,
            (o, v) => o.LeftContent = v);

    public static readonly DirectProperty<ExcelStatusBar, object?> RightContentProperty =
        AvaloniaProperty.RegisterDirect<ExcelStatusBar, object?>(
            nameof(RightContent),
            o => o.RightContent,
            (o, v) => o.RightContent = v);

    private object? _leftContent;
    private object? _rightContent;

    public object? LeftContent
    {
        get => _leftContent;
        set => SetAndRaise(LeftContentProperty, ref _leftContent, value);
    }

    public object? RightContent
    {
        get => _rightContent;
        set => SetAndRaise(RightContentProperty, ref _rightContent, value);
    }
}
