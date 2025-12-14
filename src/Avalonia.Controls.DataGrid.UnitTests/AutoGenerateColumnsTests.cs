using System;
using System.Data;
using System.Reflection;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Controls.Documents;
using Avalonia.Data.Core.Plugins;
using Avalonia.Threading;
using Xunit;

namespace Avalonia.Controls.DataGridTests
{
    public class AutoGenerateColumnsTests
    {
        [Fact]
        public void AutoGenerateColumns_Uses_DataColumn_Descriptors_For_DataTable_DefaultView()
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Id", typeof(int)));
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Balance", typeof(decimal)));
            table.Rows.Add(1, "Alice", 12.3m);

            var grid = new DataGrid
            {
                AutoGenerateColumns = true,
                ItemsSource = table.DefaultView
            };

            // Simulate having been measured so auto-generation runs.
            typeof(DataGrid)
                .GetField("_measured", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(grid, true);

            typeof(DataGrid)
                .GetMethod("AutoGenerateColumnsPrivate", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grid, null);

            Assert.Equal(3, grid.ColumnDefinitions.Count);

            Assert.Collection(
                grid.ColumnDefinitions,
                col =>
                {
                    Assert.Equal("Id", col.Header);
                    var binding = ((DataGridBoundColumn)col).Binding as Binding;
                    Assert.Equal("Id", binding?.Path);
                },
                col =>
                {
                    Assert.Equal("Name", col.Header);
                    var binding = ((DataGridBoundColumn)col).Binding as Binding;
                    Assert.Equal("Name", binding?.Path);
                },
                col =>
                {
                    Assert.Equal("Balance", col.Header);
                    var binding = ((DataGridBoundColumn)col).Binding as Binding;
                    Assert.Equal("Balance", binding?.Path);
                });
        }

        [Fact]
        public void AutoGenerateColumns_Uses_DataColumn_Descriptors_For_DataGridCollectionView_Wrapping_DataTable_DefaultView()
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn("Id", typeof(int)));
            table.Columns.Add(new DataColumn("Name", typeof(string)));
            table.Columns.Add(new DataColumn("Balance", typeof(decimal)));
            table.Rows.Add(1, "Alice", 12.3m);

            var grid = new DataGrid
            {
                AutoGenerateColumns = true,
                ItemsSource = new DataGridCollectionView(table.DefaultView)
            };

            typeof(DataGrid)
                .GetField("_measured", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(grid, true);

            typeof(DataGrid)
                .GetMethod("AutoGenerateColumnsPrivate", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grid, null);

            Assert.Equal(3, grid.ColumnDefinitions.Count);

            Assert.Collection(
                grid.ColumnDefinitions,
                col =>
                {
                    Assert.Equal("Id", col.Header);
                    var binding = ((DataGridBoundColumn)col).Binding as Binding;
                    Assert.Equal("Id", binding?.Path);
                },
                col =>
                {
                    Assert.Equal("Name", col.Header);
                    var binding = ((DataGridBoundColumn)col).Binding as Binding;
                    Assert.Equal("Name", binding?.Path);
                },
                col =>
                {
                    Assert.Equal("Balance", col.Header);
                    var binding = ((DataGridBoundColumn)col).Binding as Binding;
                    Assert.Equal("Balance", binding?.Path);
                });
        }

        [Fact]
        public void AutoGenerateColumns_Creates_ComboBox_For_Enum()
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = true,
                ItemsSource = new[] { new EnumItem { Status = EnumItemStatus.Active } }
            };

            typeof(DataGrid)
                .GetField("_measured", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(grid, true);

            typeof(DataGrid)
                .GetMethod("AutoGenerateColumnsPrivate", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grid, null);

            var column = Assert.Single(grid.ColumnDefinitions);
            var comboColumn = Assert.IsType<DataGridComboBoxColumn>(column);
            Assert.Equal("Status", comboColumn.Header);
            Assert.NotNull(comboColumn.ItemsSource);
            Assert.IsType<Binding>(comboColumn.SelectedItemBinding);
        }

        [Fact]
        public void AutoGenerateColumns_Creates_Hyperlink_For_Uri()
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = true,
                ItemsSource = new[] { new LinkItem { Link = new Uri("https://example.com") } }
            };

            typeof(DataGrid)
                .GetField("_measured", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(grid, true);

            typeof(DataGrid)
                .GetMethod("AutoGenerateColumnsPrivate", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(grid, null);

            var column = Assert.Single(grid.ColumnDefinitions);
            var hyperlinkColumn = Assert.IsType<DataGridHyperlinkColumn>(column);
            Assert.Equal("Link", hyperlinkColumn.Header);
            Assert.IsType<Binding>(hyperlinkColumn.Binding);
            Assert.IsType<Binding>(hyperlinkColumn.ContentBinding);
        }

        private sealed class EnumItem
        {
            public EnumItemStatus Status { get; set; }
        }

        private sealed class LinkItem
        {
            public Uri Link { get; set; } = new Uri("https://example.com");
        }

        private enum EnumItemStatus
        {
            Active,
            Inactive,
            Pending
        }

    }
}
