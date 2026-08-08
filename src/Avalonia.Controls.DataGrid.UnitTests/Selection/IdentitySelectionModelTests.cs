// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using Avalonia.Controls.DataGridSelection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Selection;

public sealed class IdentitySelectionModelTests
{
    [AvaloniaFact]
    public void Replacement_and_move_restore_selection_from_stored_identity_keys()
    {
        var rows = new ObservableCollection<Row>
        {
            new(1, "one"),
            new(2, "two"),
            new(3, "three")
        };
        var model = new IdentitySelectionModel(static item => ((Row)item).Id)
        {
            SingleSelect = true,
            Source = rows,
            SelectedIndex = 1
        };
        var replacement = new Row(2, "replacement");

        rows[1] = replacement;
        rows.Move(1, 2);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(2, model.SelectedIndex);
        Assert.Same(replacement, model.SelectedItem);
    }

    private sealed record Row(int Id, string Name);
}
