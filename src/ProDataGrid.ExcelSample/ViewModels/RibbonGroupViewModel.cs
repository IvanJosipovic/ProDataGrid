using System.Collections.Generic;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.ViewModels;

public sealed class RibbonGroupViewModel : ReactiveObject
{
    public RibbonGroupViewModel(string title, IReadOnlyList<IRibbonCommandViewModel> commands)
    {
        Title = title;
        Commands = commands;
    }

    public string Title { get; }

    public IReadOnlyList<IRibbonCommandViewModel> Commands { get; }
}
