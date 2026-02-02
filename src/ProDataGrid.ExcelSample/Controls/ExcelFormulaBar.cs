using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;

namespace ProDataGrid.ExcelSample.Controls;

public sealed class ExcelFormulaBar : TemplatedControl
{
    public static readonly DirectProperty<ExcelFormulaBar, string?> NameTextProperty =
        AvaloniaProperty.RegisterDirect<ExcelFormulaBar, string?>(
            nameof(NameText),
            o => o.NameText,
            (o, v) => o.NameText = v);

    public static readonly DirectProperty<ExcelFormulaBar, string?> FormulaTextProperty =
        AvaloniaProperty.RegisterDirect<ExcelFormulaBar, string?>(
            nameof(FormulaText),
            o => o.FormulaText,
            (o, v) => o.FormulaText = v);

    public static readonly DirectProperty<ExcelFormulaBar, string?> FilterTextProperty =
        AvaloniaProperty.RegisterDirect<ExcelFormulaBar, string?>(
            nameof(FilterText),
            o => o.FilterText,
            (o, v) => o.FilterText = v);

    public static readonly DirectProperty<ExcelFormulaBar, ICommand?> NameCommitCommandProperty =
        AvaloniaProperty.RegisterDirect<ExcelFormulaBar, ICommand?>(
            nameof(NameCommitCommand),
            o => o.NameCommitCommand,
            (o, v) => o.NameCommitCommand = v);

    public static readonly DirectProperty<ExcelFormulaBar, ICommand?> FormulaCommitCommandProperty =
        AvaloniaProperty.RegisterDirect<ExcelFormulaBar, ICommand?>(
            nameof(FormulaCommitCommand),
            o => o.FormulaCommitCommand,
            (o, v) => o.FormulaCommitCommand = v);

    public static readonly DirectProperty<ExcelFormulaBar, ICommand?> FormulaCancelCommandProperty =
        AvaloniaProperty.RegisterDirect<ExcelFormulaBar, ICommand?>(
            nameof(FormulaCancelCommand),
            o => o.FormulaCancelCommand,
            (o, v) => o.FormulaCancelCommand = v);

    private string? _nameText;
    private string? _formulaText;
    private string? _filterText;
    private ICommand? _nameCommitCommand;
    private ICommand? _formulaCommitCommand;
    private ICommand? _formulaCancelCommand;

    public string? NameText
    {
        get => _nameText;
        set => SetAndRaise(NameTextProperty, ref _nameText, value);
    }

    public string? FormulaText
    {
        get => _formulaText;
        set => SetAndRaise(FormulaTextProperty, ref _formulaText, value);
    }

    public string? FilterText
    {
        get => _filterText;
        set => SetAndRaise(FilterTextProperty, ref _filterText, value);
    }

    public ICommand? NameCommitCommand
    {
        get => _nameCommitCommand;
        set => SetAndRaise(NameCommitCommandProperty, ref _nameCommitCommand, value);
    }

    public ICommand? FormulaCommitCommand
    {
        get => _formulaCommitCommand;
        set => SetAndRaise(FormulaCommitCommandProperty, ref _formulaCommitCommand, value);
    }

    public ICommand? FormulaCancelCommand
    {
        get => _formulaCancelCommand;
        set => SetAndRaise(FormulaCancelCommandProperty, ref _formulaCancelCommand, value);
    }
}
