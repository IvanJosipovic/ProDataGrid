using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.DataGridSearching;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Searching;

public class DataGridSearchAdapterTests
{
    [AvaloniaFact]
    public void ValueAccessor_Is_Used_When_No_Path()
    {
        var items = new[]
        {
            new Person("Alpha"),
            new Person("Beta")
        };
        var view = new DataGridCollectionView(items);
        var model = new SearchModel();

        var column = new DataGridTextColumn();
        DataGridColumnMetadata.SetValueAccessor(column, new DataGridColumnValueAccessor<Person, string>(p => p.Name));

        var adapter = new DataGridSearchAdapter(model, () => new[] { column });
        adapter.AttachView(view);

        model.SetOrUpdate(new SearchDescriptor("Beta", comparison: StringComparison.OrdinalIgnoreCase));

        var result = Assert.Single(model.Results);
        Assert.Same(items[1], result.Item);
        Assert.Same(column, result.ColumnId);
    }

    [AvaloniaFact]
    public void Column_Definition_Id_Is_Selected()
    {
        var items = new[]
        {
            new Person("Alpha"),
            new Person("Beta")
        };
        var view = new DataGridCollectionView(items);
        var model = new SearchModel();

        var definition = new DataGridTextColumnDefinition
        {
            Header = "Name",
            Binding = DataGridBindingDefinition.Create<Person, string>(p => p.Name)
        };

        var grid = new DataGrid
        {
            ColumnDefinitionsSource = new[] { definition }
        };

        var adapter = new DataGridSearchAdapter(model, () => grid.Columns);
        adapter.AttachView(view);

        model.SetOrUpdate(new SearchDescriptor(
            "Beta",
            scope: SearchScope.ExplicitColumns,
            columnIds: new object[] { definition },
            comparison: StringComparison.OrdinalIgnoreCase));

        var result = Assert.Single(model.Results);
        Assert.Same(items[1], result.Item);
    }

    [AvaloniaFact]
    public void Collection_Changes_Are_Coalesced_To_Single_Refresh()
    {
        var items = new ObservableCollection<Person>
        {
            new("Alpha")
        };
        var view = new DataGridCollectionView(items);
        var model = new SearchModel();

        var adapter = new CountingSearchAdapter(model);
        adapter.AttachView(view);

        model.SetOrUpdate(new SearchDescriptor("Alpha", comparison: StringComparison.OrdinalIgnoreCase));
        adapter.ResetApplyCount();

        items.Add(new Person("Alpha"));
        items.Add(new Person("Alpha"));
        items.Add(new Person("Alpha"));

        Assert.Equal(0, adapter.ApplyCount);

        Dispatcher.UIThread.RunJobs();

        Assert.Equal(1, adapter.ApplyCount);
    }

    private sealed class CountingSearchAdapter : DataGridSearchAdapter
    {
        public CountingSearchAdapter(ISearchModel model)
            : base(model, () => Array.Empty<DataGridColumn>())
        {
        }

        public int ApplyCount { get; private set; }

        public void ResetApplyCount()
        {
            ApplyCount = 0;
        }

        protected override bool TryApplyModelToView(
            IReadOnlyList<SearchDescriptor> descriptors,
            IReadOnlyList<SearchDescriptor> previousDescriptors,
            out IReadOnlyList<SearchResult> results)
        {
            ApplyCount++;
            results = Array.Empty<SearchResult>();
            return true;
        }
    }

    private sealed class Person
    {
        public Person(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
