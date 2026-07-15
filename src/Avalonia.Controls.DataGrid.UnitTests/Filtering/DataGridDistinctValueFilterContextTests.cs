// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls.DataGridFiltering;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Filtering;

public class DataGridDistinctValueFilterContextTests
{
    [Fact]
    public void Refresh_Builds_Sorted_Distinct_Values_With_Counts_Unchecked()
    {
        var model = new FilteringModel();
        var context = CreateContext(model);

        context.Refresh(new[]
        {
            new Item("Beta"),
            new Item("Alpha"),
            new Item("Beta"),
            new Item(null)
        });

        Assert.Collection(
            context.Options,
            option => AssertOption(option, "(Empty)", 1, false),
            option => AssertOption(option, "Alpha", 1, false),
            option => AssertOption(option, "Beta", 2, false));
        Assert.Empty(model.Descriptors);
    }

    [Fact]
    public void Search_Filters_Display_Options_By_Substring_Without_Changing_Selection()
    {
        var model = new FilteringModel();
        var context = CreateContext(model);
        context.Refresh(new[]
        {
            new Item("Alpha"),
            new Item("Beta"),
            new Item("Alphabet")
        });

        context.Options.Single(option => option.Display == "Beta").IsSelected = true;
        context.SearchText = "ALP";

        Assert.Equal(new[] { "Alpha", "Alphabet" }, context.Options.Select(option => option.Display));
        FilteringDescriptor descriptor = Assert.Single(model.Descriptors);
        Assert.Equal(FilteringOperator.In, descriptor.Operator);
        Assert.Equal("Beta", Assert.Single(descriptor.Values));
    }

    [Fact]
    public void Selecting_Values_Creates_In_Descriptor_And_Unchecking_All_Removes_It()
    {
        var model = new FilteringModel();
        var context = CreateContext(model);
        context.Refresh(new[]
        {
            new Item("Alpha"),
            new Item("Beta"),
            new Item("Gamma")
        });

        IFilterDistinctValueOption alpha = context.Options.Single(option => option.Display == "Alpha");
        IFilterDistinctValueOption gamma = context.Options.Single(option => option.Display == "Gamma");
        alpha.IsSelected = true;
        gamma.IsSelected = true;

        FilteringDescriptor descriptor = Assert.Single(model.Descriptors);
        Assert.Equal("Name", descriptor.ColumnId);
        Assert.Equal(nameof(Item.Name), descriptor.PropertyPath);
        Assert.Equal(FilteringOperator.In, descriptor.Operator);
        Assert.Equal(new object[] { "Alpha", "Gamma" }, descriptor.Values);

        alpha.IsSelected = false;
        gamma.IsSelected = false;

        Assert.Empty(model.Descriptors);
        Assert.All(context.Options, option => Assert.False(option.IsSelected));
    }

    [Fact]
    public void Refresh_Restores_Selected_Values_From_Active_Descriptor()
    {
        var model = new FilteringModel();
        model.SetOrUpdate(new FilteringDescriptor(
            "Name",
            FilteringOperator.In,
            nameof(Item.Name),
            values: new object[] { "Beta" }));
        var context = CreateContext(model);

        context.Refresh(new[]
        {
            new Item("Alpha"),
            new Item("Beta"),
            new Item("Beta")
        });

        Assert.False(context.Options.Single(option => option.Display == "Alpha").IsSelected);
        IFilterDistinctValueOption beta = context.Options.Single(option => option.Display == "Beta");
        Assert.True(beta.IsSelected);
        Assert.Equal(2, beta.Count);
        Assert.Single(model.Descriptors);
    }

    [Fact]
    public void Refresh_Prefers_Exact_Column_Descriptor_When_Property_Path_Is_Shared()
    {
        IFilteringModel model = new ReadOnlyFilteringModel(new[]
        {
            new FilteringDescriptor(
                "FirstNameColumn",
                FilteringOperator.In,
                nameof(Item.Name),
                values: new object[] { "Alpha" }),
            new FilteringDescriptor(
                "Name",
                FilteringOperator.In,
                nameof(Item.Name),
                values: new object[] { "Beta" })
        });
        var context = CreateContext(model);

        context.Refresh(new[]
        {
            new Item("Alpha"),
            new Item("Beta")
        });

        Assert.False(context.Options.Single(option => option.Display == "Alpha").IsSelected);
        Assert.True(context.Options.Single(option => option.Display == "Beta").IsSelected);
    }

    [Fact]
    public void Property_Path_Fallback_Updates_And_Removes_The_Matched_Descriptor_Identity()
    {
        var model = new FilteringModel();
        model.SetOrUpdate(new FilteringDescriptor(
            "LegacyNameColumn",
            FilteringOperator.In,
            nameof(Item.Name),
            values: new object[] { "Beta" }));
        var context = CreateContext(model);

        context.Refresh(new[]
        {
            new Item("Alpha"),
            new Item("Beta")
        });

        IFilterDistinctValueOption alpha = context.Options.Single(option => option.Display == "Alpha");
        IFilterDistinctValueOption beta = context.Options.Single(option => option.Display == "Beta");
        Assert.True(beta.IsSelected);

        alpha.IsSelected = true;

        FilteringDescriptor descriptor = Assert.Single(model.Descriptors);
        Assert.Equal("LegacyNameColumn", descriptor.ColumnId);
        Assert.Equal(new object[] { "Alpha", "Beta" }, descriptor.Values);

        alpha.IsSelected = false;
        beta.IsSelected = false;

        Assert.Empty(model.Descriptors);
    }

    [Fact]
    public void Custom_Comparer_Groups_And_Filters_Values_Consistently()
    {
        var model = new FilteringModel();
        var context = new DataGridDistinctValueFilterContext(
            model,
            "Name",
            new DataGridColumnValueAccessor<Item, string?>(item => item.Name),
            "Name",
            nameof(Item.Name),
            StringComparer.OrdinalIgnoreCase);
        context.Refresh(new[]
        {
            new Item("Alpha"),
            new Item("ALPHA"),
            new Item("Beta")
        });

        DataGridDistinctValueFilterOption alpha = Assert.IsType<DataGridDistinctValueFilterOption>(
            context.Options.Single(option => option.Display == "Alpha"));
        Assert.Equal(2, alpha.Count);
        Assert.Equal("Alpha", alpha.Value);
        alpha.IsSelected = true;

        FilteringDescriptor descriptor = Assert.Single(model.Descriptors);
        Assert.NotNull(descriptor.Predicate);
        Assert.True(descriptor.Predicate(new Item("alpha")));
        Assert.False(descriptor.Predicate(new Item("Beta")));
    }

    private static DataGridDistinctValueFilterContext CreateContext(IFilteringModel model)
    {
        return new DataGridDistinctValueFilterContext(
            model,
            "Name",
            new DataGridColumnValueAccessor<Item, string?>(item => item.Name),
            "Name",
            nameof(Item.Name));
    }

    private static void AssertOption(
        IFilterDistinctValueOption option,
        string display,
        int count,
        bool isSelected)
    {
        Assert.Equal(display, option.Display);
        Assert.Equal(count, option.Count);
        Assert.Equal(isSelected, option.IsSelected);
    }

    private sealed record Item(string? Name);

    private sealed class ReadOnlyFilteringModel : IFilteringModel
    {
        public ReadOnlyFilteringModel(IReadOnlyList<FilteringDescriptor> descriptors)
        {
            Descriptors = descriptors;
        }

        public IReadOnlyList<FilteringDescriptor> Descriptors { get; }

        public bool OwnsViewFilter { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<FilteringChangedEventArgs>? FilteringChanged
        {
            add { }
            remove { }
        }

        public event EventHandler<FilteringChangingEventArgs>? FilteringChanging
        {
            add { }
            remove { }
        }

        public void SetOrUpdate(FilteringDescriptor descriptor) => throw new NotSupportedException();

        public void Apply(IEnumerable<FilteringDescriptor> descriptors) => throw new NotSupportedException();

        public void Clear() => throw new NotSupportedException();

        public bool Remove(object columnId) => throw new NotSupportedException();

        public bool Move(object columnId, int newIndex) => throw new NotSupportedException();

        public void BeginUpdate() => throw new NotSupportedException();

        public void EndUpdate() => throw new NotSupportedException();

        public IDisposable DeferRefresh() => throw new NotSupportedException();
    }
}
