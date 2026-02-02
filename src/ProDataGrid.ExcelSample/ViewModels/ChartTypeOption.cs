using ProCharts;

namespace ProDataGrid.ExcelSample.ViewModels;

public sealed class ChartTypeOption
{
    public ChartTypeOption(string label, ChartSeriesKind seriesKind, bool isSingleSeries = false)
    {
        Label = label;
        SeriesKind = seriesKind;
        IsSingleSeries = isSingleSeries;
    }

    public string Label { get; }

    public ChartSeriesKind SeriesKind { get; }

    public bool IsSingleSeries { get; }
}
