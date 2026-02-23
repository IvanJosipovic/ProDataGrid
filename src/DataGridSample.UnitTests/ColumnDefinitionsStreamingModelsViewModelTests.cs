using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class ColumnDefinitionsStreamingModelsViewModelTests
{
    [Fact]
    public void FastPathOptions_Enable_HighPerformance_Searching_For_Streaming_Sample()
    {
        var viewModel = new ColumnDefinitionsStreamingModelsViewModel();

        Assert.True(viewModel.FastPathOptions.UseAccessorsOnly);
        Assert.True(viewModel.FastPathOptions.EnableHighPerformanceSearching);
        Assert.False(viewModel.FastPathOptions.HighPerformanceSearchTrackItemChanges);
    }
}
