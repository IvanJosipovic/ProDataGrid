using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using DataGridSample.Models;
using ReactiveUI;

namespace DataGridSample.ViewModels;

public sealed class HeaderSelectionViewModel : ReactiveObject
{
    private DataGridSelectionUnit _selectionUnit = DataGridSelectionUnit.CellOrRowOrColumnHeader;
    private DataGridSelectionMode _selectionMode = DataGridSelectionMode.Extended;
    private bool _canUserSelectRows = true;
    private bool _canUserSelectColumns = true;

    public HeaderSelectionViewModel()
    {
        Items = new ObservableCollection<Country>(Countries.All.Take(18).ToList());
        SelectionModes = Enum.GetValues<DataGridSelectionMode>();
        SelectionUnits = new[]
        {
            DataGridSelectionUnit.Cell,
            DataGridSelectionUnit.CellOrRowHeader,
            DataGridSelectionUnit.CellOrColumnHeader,
            DataGridSelectionUnit.CellOrRowOrColumnHeader,
            DataGridSelectionUnit.FullRow
        };
    }

    public ObservableCollection<Country> Items { get; }

    public Array SelectionModes { get; }

    public DataGridSelectionUnit[] SelectionUnits { get; }

    public DataGridSelectionUnit SelectionUnit
    {
        get => _selectionUnit;
        set => this.RaiseAndSetIfChanged(ref _selectionUnit, value);
    }

    public DataGridSelectionMode SelectionMode
    {
        get => _selectionMode;
        set => this.RaiseAndSetIfChanged(ref _selectionMode, value);
    }

    public bool CanUserSelectRows
    {
        get => _canUserSelectRows;
        set => this.RaiseAndSetIfChanged(ref _canUserSelectRows, value);
    }

    public bool CanUserSelectColumns
    {
        get => _canUserSelectColumns;
        set => this.RaiseAndSetIfChanged(ref _canUserSelectColumns, value);
    }
}
