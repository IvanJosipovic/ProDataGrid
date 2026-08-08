// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Filtering;

public class DataGridHierarchicalFilteringAdapterTests
{
    [Theory]
    [InlineData(DataGridHierarchyFilterPolicy.SelfOnly, "needle")]
    [InlineData(DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches, "root,branch,needle")]
    [InlineData(DataGridHierarchyFilterPolicy.KeepDescendantsOfMatches, "needle")]
    [InlineData(
        DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches |
        DataGridHierarchyFilterPolicy.KeepDescendantsOfMatches,
        "root,branch,needle")]
    public void Applies_Explicit_Relative_Policy(
        DataGridHierarchyFilterPolicy policy,
        string expected)
    {
        TreeItem root = CreateTree();
        HierarchicalModel hierarchy = CreateModel(root);
        using AdapterFixture fixture = CreateFixture(hierarchy, policy);

        fixture.Filter("needle");

        Assert.Equal(expected.Split(','), fixture.VisibleNames());
        Assert.Equal(4, hierarchy.Count);
        Assert.True(hierarchy.Root!.IsExpanded);
    }

    [Fact]
    public void Descendant_Policy_Keeps_The_Materialized_Subtree_Of_A_Match()
    {
        TreeItem root = CreateTree();
        HierarchicalModel hierarchy = CreateModel(root);
        using AdapterFixture fixture = CreateFixture(
            hierarchy,
            DataGridHierarchyFilterPolicy.KeepDescendantsOfMatches);

        fixture.Filter("branch");

        Assert.Equal(new[] { "branch", "needle" }, fixture.VisibleNames());
    }

    [Fact]
    public void Clearing_Filter_Restores_View_Without_Changing_Expansion()
    {
        TreeItem root = CreateTree();
        HierarchicalModel hierarchy = CreateModel(root);
        using AdapterFixture fixture = CreateFixture(
            hierarchy,
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);
        fixture.Filter("needle");

        fixture.FilteringModel.Clear();

        Assert.Equal(new[] { "root", "branch", "needle", "other" }, fixture.VisibleNames());
        Assert.True(hierarchy.Root!.IsExpanded);
        Assert.True(hierarchy.Root.Children[0].IsExpanded);
    }

    [Fact]
    public void Collapsed_Tree_Uses_Materialized_Descendants_Without_Expanding()
    {
        TreeItem root = CreateTree();
        HierarchicalModel hierarchy = CreateModel(root, virtualizeChildren: false);
        HierarchicalNode branch = hierarchy.Root!.Children[0];
        hierarchy.Collapse(branch);
        using AdapterFixture fixture = CreateFixture(
            hierarchy,
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);

        fixture.Filter("needle");

        Assert.Equal(new[] { "root", "branch" }, fixture.VisibleNames());
        Assert.False(branch.IsExpanded);
    }

    [Fact]
    public void Filtering_Does_Not_Implicitly_Load_Unmaterialized_Children()
    {
        int loadCount = 0;
        var root = new TreeItem("root");
        var hierarchy = new HierarchicalModel(new HierarchicalOptions
        {
            ChildrenSelectorAsync = (_, _) =>
            {
                loadCount++;
                return Task.FromResult<IEnumerable?>(new[] { new TreeItem("needle") });
            },
        });
        hierarchy.SetRoot(root);
        using AdapterFixture fixture = CreateFixture(
            hierarchy,
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);

        fixture.Filter("needle");

        Assert.Equal(0, loadCount);
        Assert.Empty(fixture.VisibleNames());
        Assert.False(hierarchy.Root!.IsExpanded);
    }

    [Fact]
    public void Observable_Child_Addition_Rebuilds_One_Coherent_Filtered_View()
    {
        var root = new TreeItem("root");
        root.Children.Add(new TreeItem("other"));
        HierarchicalModel hierarchy = CreateModel(root);
        using AdapterFixture fixture = CreateFixture(
            hierarchy,
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);
        fixture.Filter("needle");
        Assert.Empty(fixture.VisibleNames());

        root.Children.Add(new TreeItem("needle"));

        Assert.Equal(new[] { "root", "needle" }, fixture.VisibleNames());
    }

    [Fact]
    public async Task Async_Completion_Reevaluates_Ancestor_Paths()
    {
        var completion = new TaskCompletionSource<IEnumerable?>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var root = new TreeItem("root");
        var hierarchy = new HierarchicalModel(new HierarchicalOptions
        {
            ChildrenSelectorAsync = (item, _) =>
                ReferenceEquals(item, root)
                    ? completion.Task
                    : Task.FromResult<IEnumerable?>(null),
        });
        hierarchy.SetRoot(root);
        using AdapterFixture fixture = CreateFixture(
            hierarchy,
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);
        fixture.Filter("needle");

        Task expand = hierarchy.ExpandAsync(hierarchy.Root!);
        completion.SetResult(new[] { new TreeItem("needle") });
        await expand;

        Assert.Equal(new[] { "root", "needle" }, fixture.VisibleNames());
    }

    [Fact]
    public void Supports_Node_Typed_Column_Accessors_For_Compatibility()
    {
        TreeItem root = CreateTree();
        HierarchicalModel hierarchy = CreateModel(root);
        var column = new DataGridTextColumn { Header = "Name" };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<HierarchicalNode, string>(
                static node => ((TreeItem)node.Item).Name));
        using var fixture = new AdapterFixture(
            hierarchy,
            column,
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);

        fixture.Filter("needle");

        Assert.Equal(new[] { "root", "branch", "needle" }, fixture.VisibleNames());
    }

    [Fact]
    public void Custom_Predicate_Receives_The_Underlying_Item()
    {
        TreeItem root = CreateTree();
        HierarchicalModel hierarchy = CreateModel(root);
        using AdapterFixture fixture = CreateFixture(
            hierarchy,
            DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches);
        object? observed = null;

        fixture.FilteringModel.SetOrUpdate(new FilteringDescriptor(
            columnId: fixture.Column,
            @operator: FilteringOperator.Custom,
            predicate: item =>
            {
                observed = item;
                return ((TreeItem)item).Name == "needle";
            }));

        Assert.IsType<TreeItem>(observed);
        Assert.Equal(new[] { "root", "branch", "needle" }, fixture.VisibleNames());
    }

    [AvaloniaFact]
    public void Factory_Follows_The_Grid_Current_Hierarchy_Model()
    {
        HierarchicalModel first = CreateModel(CreateTree());
        HierarchicalModel second = CreateModel(CreateTree());
        var factory = new DataGridHierarchicalFilteringAdapterFactory
        {
            Policy = DataGridHierarchyFilterPolicy.KeepAncestorsOfMatches |
                DataGridHierarchyFilterPolicy.KeepDescendantsOfMatches,
        };
        var grid = new DataGrid
        {
            HierarchicalRowsEnabled = true,
            HierarchicalModel = first,
            FilteringAdapterFactory = factory,
        };

        using DataGridFilteringAdapter adapter = factory.Create(grid, new FilteringModel());
        Assert.IsType<DataGridHierarchicalFilteringAdapter>(adapter);

        grid.HierarchicalModel = second;
        Assert.Same(second, grid.HierarchicalModel);
    }

    [Fact]
    public void Rejects_Unsupported_Policy_Flags()
    {
        HierarchicalModel hierarchy = CreateModel(CreateTree());
        var model = new FilteringModel();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DataGridHierarchicalFilteringAdapter(
                model,
                static () => Array.Empty<DataGridColumn>(),
                hierarchy,
                (DataGridHierarchyFilterPolicy)4));
    }

    private static TreeItem CreateTree()
    {
        var root = new TreeItem("root");
        var branch = new TreeItem("branch");
        branch.Children.Add(new TreeItem("needle"));
        root.Children.Add(branch);
        root.Children.Add(new TreeItem("other"));
        return root;
    }

    private static HierarchicalModel CreateModel(
        TreeItem root,
        bool virtualizeChildren = true)
    {
        var model = new HierarchicalModel(new HierarchicalOptions
        {
            ChildrenSelector = static item => ((TreeItem)item).Children,
            IsLeafSelector = static item => ((TreeItem)item).Children.Count == 0,
            VirtualizeChildren = virtualizeChildren,
        });
        model.SetRoot(root);
        model.ExpandAll();
        return model;
    }

    private static AdapterFixture CreateFixture(
        HierarchicalModel hierarchy,
        DataGridHierarchyFilterPolicy policy)
    {
        var column = new DataGridTextColumn { Header = "Name" };
        DataGridColumnMetadata.SetValueAccessor(
            column,
            new DataGridColumnValueAccessor<TreeItem, string>(static item => item.Name));
        return new AdapterFixture(hierarchy, column, policy);
    }

    private sealed class AdapterFixture : System.IDisposable
    {
        private readonly DataGridColumn _column;
        private readonly DataGridCollectionView _view;
        private readonly DataGridHierarchicalFilteringAdapter _adapter;

        public AdapterFixture(
            HierarchicalModel hierarchy,
            DataGridColumn column,
            DataGridHierarchyFilterPolicy policy)
        {
            _column = column;
            FilteringModel = new FilteringModel();
            _view = new DataGridCollectionView(hierarchy.ObservableFlattened);
            _adapter = new DataGridHierarchicalFilteringAdapter(
                FilteringModel,
                () => new[] { _column },
                hierarchy,
                policy,
                new DataGridFastPathOptions { UseAccessorsOnly = true });
            _adapter.AttachView(_view);
        }

        public FilteringModel FilteringModel { get; }

        public DataGridColumn Column => _column;

        public void Filter(string value)
        {
            FilteringModel.SetOrUpdate(new FilteringDescriptor(
                _column,
                FilteringOperator.Contains,
                value: value));
        }

        public string[] VisibleNames()
        {
            return _view.Cast<HierarchicalNode>()
                .Select(static node => ((TreeItem)node.Item).Name)
                .ToArray();
        }

        public void Dispose()
        {
            _adapter.Dispose();
        }
    }

    private sealed class TreeItem
    {
        public TreeItem(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public ObservableCollection<TreeItem> Children { get; } = new();
    }
}
