using System;
using System.Collections.ObjectModel;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public class RecycleDiagnosticsViewModel : ObservableObject
{
    private int _realizedRows;
    private int _recycledRows;
    private double _viewportHeight;

    public RecycleDiagnosticsViewModel()
    {
        Items = new ObservableCollection<PixelItem>();
        Populate(500);
    }

    public ObservableCollection<PixelItem> Items { get; }

    public int RealizedRows
    {
        get => _realizedRows;
        set => SetProperty(ref _realizedRows, value);
    }

    public int RecycledRows
    {
        get => _recycledRows;
        set => SetProperty(ref _recycledRows, value);
    }

    public double ViewportHeight
    {
        get => _viewportHeight;
        set => SetProperty(ref _viewportHeight, value);
    }

    private void Populate(int count)
    {
        Items.Clear();

        var random = new Random(17);
        for (int i = 1; i <= count; i++)
        {
            Items.Add(PixelItem.Create(i, random));
        }
    }
}
