// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSorting;
using Xunit;

namespace Avalonia.Controls.DataGridTests.ColumnDefinitions;

public sealed class DataGridGeneratedCollectionViewOperationsTests
{
    [Fact]
    public void ApplySorting_installs_compiled_comparer_and_clears_without_descriptors()
    {
        Row[] rows = [new Row(3), new Row(1), new Row(2)];
        var view = new DataGridCollectionView(rows);
        var compiler = new RowSortingCompiler();
        SortingDescriptor[] descriptors = [new SortingDescriptor("id", System.ComponentModel.ListSortDirection.Ascending)];

        DataGridGeneratedCollectionViewOperations.ApplySorting(view, compiler, descriptors);

        Assert.Equal(new[] { 1, 2, 3 }, view.Cast<Row>().Select(static row => row.Id));
        Assert.Single(view.SortDescriptions);
        Assert.Equal(1, compiler.CompileCount);

        DataGridGeneratedCollectionViewOperations.ApplySorting(view, compiler, Array.Empty<SortingDescriptor>());

        Assert.Empty(view.SortDescriptions);
    }

    [Fact]
    public void ApplySorting_validates_dependencies()
    {
        var view = new DataGridCollectionView(Array.Empty<Row>());
        var compiler = new RowSortingCompiler();

        Assert.Throws<ArgumentNullException>(() =>
            DataGridGeneratedCollectionViewOperations.ApplySorting<Row>(null!, compiler, Array.Empty<SortingDescriptor>()));
        Assert.Throws<ArgumentNullException>(() =>
            DataGridGeneratedCollectionViewOperations.ApplySorting<Row>(view, null!, Array.Empty<SortingDescriptor>()));
        Assert.Throws<ArgumentNullException>(() =>
            DataGridGeneratedCollectionViewOperations.ApplySorting(view, compiler, null!));
    }

    private sealed record Row(int Id);

    private sealed class RowSortingCompiler : IDataGridSortingCompiler<Row>
    {
        public int CompileCount { get; private set; }

        public IComparer<Row> CreateSortComparer(IReadOnlyList<SortingDescriptor> descriptors)
        {
            CompileCount++;
            return Comparer<Row>.Create(static (left, right) => left.Id.CompareTo(right.Id));
        }
    }
}
