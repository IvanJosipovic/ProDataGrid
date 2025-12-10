// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls.DataGridSelection;
using Avalonia.Controls.Selection;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public class DataGridSelectionModelAdapterTests
{
    [Fact]
    public void Adapter_Delegates_To_Model()
    {
        var model = new SelectionModel<string> { SingleSelect = false };
        model.Source = new[] { "a", "b", "c" };

        var adapter = new DataGridSelectionModelAdapter(model);

        adapter.SelectRange(0, 1);
        Assert.True(adapter.IsSelected(0));
        Assert.True(adapter.IsSelected(1));
        Assert.False(adapter.IsSelected(2));

        adapter.Deselect(0);
        Assert.False(adapter.IsSelected(0));

        adapter.Clear();
        Assert.False(adapter.IsSelected(1));
    }

    [Fact]
    public void SelectedItemsView_Reflects_Model_Selection()
    {
        var model = new SelectionModel<string> { SingleSelect = false };
        model.Source = new[] { "a", "b", "c" };

        var adapter = new DataGridSelectionModelAdapter(model);
        adapter.SelectRange(1, 2);

        Assert.Equal(2, adapter.SelectedItemsView.Count);
        Assert.Equal("b", adapter.SelectedItemsView[0]);
        Assert.Equal("c", adapter.SelectedItemsView[1]);
    }
}
