using Avalonia.Controls;
using DataGridSample.CustomDrawing;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class CustomDrawingEditingSourceGenerationTests
{
    [Fact]
    public void ViewModel_uses_generated_built_in_drawn_and_custom_drawing_definitions()
    {
        var viewModel = new CustomDrawingEditingViewModel();

        Assert.Equal(4, viewModel.ColumnDefinitions.Count);
        var id = Assert.IsType<DataGridNumericColumnDefinition>(viewModel.ColumnDefinitions[0]);
        var title = Assert.IsType<DataGridTextColumnDefinition>(viewModel.ColumnDefinitions[1]);
        Assert.Equal(DataGridColumnDisplayMode.Drawn, id.DisplayMode);
        Assert.Equal(DataGridColumnDisplayMode.Drawn, title.DisplayMode);

        for (int index = 2; index < viewModel.ColumnDefinitions.Count; index++)
        {
            var definition = Assert.IsType<DataGridCustomDrawingColumnDefinition>(viewModel.ColumnDefinitions[index]);
            Assert.IsType<SkiaTextCellDrawOperationFactory>(definition.DrawOperationFactory);
            Assert.Equal(DataGridCustomDrawingMode.DrawOperation, definition.DrawingMode);
            Assert.Equal(DataGridCustomDrawingRenderBackend.CompositionCustomVisual, definition.RenderBackend);
            Assert.Equal(DataGridCustomDrawingTextLayoutCacheMode.Shared, definition.TextLayoutCacheMode);
            Assert.Equal(1024, definition.SharedTextLayoutCacheCapacity);
            Assert.True(definition.DrawOperationLayoutFastPath);
            Assert.True(definition.UseDirectValueAccessor);
        }
    }

    [Fact]
    public void Row_uses_generated_bounded_slot_cache()
    {
        Assert.Equal(0, CustomDrawingEditingRow.NotesCellDrawCacheSlot);
        Assert.Equal(1, CustomDrawingEditingRow.CategoryCellDrawCacheSlot);

        var row = new CustomDrawingEditingRow();
        IDataGridCellDrawOperationItemCache cache = row;
        cache.SetCellDrawCacheEntry(CustomDrawingEditingRow.NotesCellDrawCacheSlot, 17, "metrics");

        Assert.True(cache.TryGetCellDrawCacheEntry(
            CustomDrawingEditingRow.NotesCellDrawCacheSlot,
            17,
            out object cached));
        Assert.Equal("metrics", cached);

        row.ClearGeneratedCellDrawCache(CustomDrawingEditingRow.NotesCellDrawCacheSlot);
        Assert.False(cache.TryGetCellDrawCacheEntry(
            CustomDrawingEditingRow.NotesCellDrawCacheSlot,
            17,
            out _));

        cache.SetCellDrawCacheEntry(2, 18, "outside-bound");
        Assert.False(cache.TryGetCellDrawCacheEntry(2, 18, out _));
    }
}
