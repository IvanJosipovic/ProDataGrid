using System.Collections.Generic;
using ReactiveUI;

namespace ProDataGrid.ExcelSample.ViewModels;

public sealed class RibbonTabViewModel : ReactiveObject
{
    public RibbonTabViewModel(string title, IReadOnlyList<RibbonGroupViewModel> groups)
    {
        Title = title;
        Groups = groups;
    }

    public string Title { get; }

    public IReadOnlyList<RibbonGroupViewModel> Groups { get; }
}
