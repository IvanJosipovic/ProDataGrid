using System.Windows.Input;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.ViewModels;

public sealed class RibbonCommandViewModel : ReactiveObject, IRibbonCommandViewModel
{
    public RibbonCommandViewModel(string label, ICommand command, string? glyph = null)
    {
        Label = label;
        Command = command;
        Glyph = glyph;
    }

    public string Label { get; }

    public string? Glyph { get; }

    public ICommand Command { get; }
}
