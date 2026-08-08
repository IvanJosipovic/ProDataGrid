// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using ProCharts;
using ProDataGrid.Charting;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Charting
{
    public sealed class DataGridChartModelTests
    {
        [Fact]
        public void GeneratedChartAdapter_Uses_Direct_Category_Value_And_Companion_Selectors()
        {
            Func<object, double?> directRevenue = static item => item is SampleRow row ? row.Revenue : null;
            var fields = new IDataGridGeneratedAnalyticsField[]
            {
                new DataGridGeneratedAnalyticsField<SampleRow, string>(
                    "category", DataGridGeneratedAnalyticsRole.ChartCategory, 0, static row => row.Category),
                new DataGridGeneratedAnalyticsField<SampleRow, double>(
                    "revenue", DataGridGeneratedAnalyticsRole.ChartValue, 0, static row => row.Revenue,
                    name: "Revenue", format: "N1", aggregate: (int)DataGridAggregateType.Average,
                    numericValueSelector: directRevenue),
                new DataGridGeneratedAnalyticsField<SampleRow, double>(
                    "cost", DataGridGeneratedAnalyticsRole.ChartXValue, 0, static row => row.Cost,
                    name: "Revenue")
            };
            var items = new[]
            {
                new SampleRow { Category = "A", Revenue = 10d, Cost = 3d },
                new SampleRow { Category = "B", Revenue = 20d, Cost = 8d }
            };

            DataGridChartModel model = DataGridGeneratedChartAdapter.CreateModel(items, fields);
            ChartDataSnapshot snapshot = model.BuildSnapshot(new ChartDataRequest());

            Assert.Equal(new[] { "A", "B" }, snapshot.Categories);
            Assert.Equal(new double?[] { 10d, 20d }, snapshot.Series[0].Values);
            Assert.Equal(new[] { 3d, 8d }, snapshot.Series[0].XValues);
            Assert.Same(directRevenue, model.Series[0].ValueSelector);
            Assert.Equal(DataGridChartAggregation.Average, model.Series[0].Aggregation);
            Assert.Equal(10d.ToString("N1", System.Globalization.CultureInfo.CurrentCulture), model.Series[0].DataLabelFormatter!(10d));
        }

        [Fact]
        public void GeneratedChartAdapter_Projects_Bounded_Spreadsheet_Range()
        {
            IDataGridGeneratedAnalyticsField[] fields = CreateGeneratedFields();
            var columns = new DataGridColumnDefinition[]
            {
                new DataGridTextColumnDefinition { ColumnKey = "category" },
                new DataGridNumericColumnDefinition { ColumnKey = "revenue" },
                new DataGridNumericColumnDefinition { ColumnKey = "cost" }
            };
            var items = new ObservableCollection<SampleRow>
            {
                new() { Id = 1, Category = "A", Revenue = 10d, Cost = 3d },
                new() { Id = 2, Category = "B", Revenue = 20d, Cost = 8d },
                new() { Id = 3, Category = "C", Revenue = 30d, Cost = 9d }
            };

            using DataGridGeneratedChartRangeProjection projection = DataGridGeneratedChartAdapter.CreateRangeProjection(
                items,
                fields,
                columns,
                new DataGridCellRange(1, 2, 0, 1),
                maximumRows: 8);

            Assert.Equal(new[] { "B", "C" }, projection.Model.Snapshot.Categories);
            ChartSeriesSnapshot series = Assert.Single(projection.Model.Snapshot.Series);
            Assert.Equal("Revenue", series.Name);
            Assert.Equal(new double?[] { 20d, 30d }, series.Values);
            Assert.Null(projection.DataSource.Series[0].ValuePath);

            projection.UpdateRange(new DataGridCellRange(0, 0, 0, 1));
            Assert.Equal(new[] { "A" }, projection.Model.Snapshot.Categories);
            Assert.Equal(new double?[] { 10d }, projection.Model.Snapshot.Series[0].Values);
        }

        [Fact]
        public void GeneratedChartAdapter_Rejects_Unbounded_Or_NonValue_Ranges()
        {
            IDataGridGeneratedAnalyticsField[] fields = CreateGeneratedFields();
            var columns = new DataGridColumnDefinition[]
            {
                new DataGridTextColumnDefinition { ColumnKey = "category" },
                new DataGridNumericColumnDefinition { ColumnKey = "revenue" }
            };
            SampleRow[] items = { new() { Id = 1, Category = "A", Revenue = 1d } };

            Assert.Throws<ArgumentOutOfRangeException>(() => DataGridGeneratedChartAdapter.CreateRangeProjection(
                items, fields, columns, new DataGridCellRange(0, 4, 0, 1), maximumRows: 4));
            Assert.Throws<ArgumentException>(() => DataGridGeneratedChartAdapter.CreateRangeProjection(
                items, fields, columns, new DataGridCellRange(0, 0, 0, 0)));
        }

        [Fact]
        public void GeneratedChartKeyMap_Tracks_Observable_List_Changes_Incrementally()
        {
            var items = new ObservableCollection<SampleRow>
            {
                new() { Id = 1 },
                new() { Id = 2 },
                new() { Id = 3 }
            };
            using var keyMap = new DataGridGeneratedListChartKeyMap<SampleRow, int>(items, SampleRowKey.Instance);

            items.Move(2, 0);
            items[1] = new SampleRow { Id = 4 };
            items.RemoveAt(2);
            items.Add(new SampleRow { Id = 5 });

            Assert.True(keyMap.TryGetKey(0, out int first));
            Assert.Equal(3, first);
            Assert.True(keyMap.TryGetIndex(4, out int replacementIndex));
            Assert.Equal(1, replacementIndex);
            Assert.True(keyMap.TryGetIndex(5, out int addedIndex));
            Assert.Equal(2, addedIndex);
            Assert.False(keyMap.TryGetIndex(1, out _));
        }

        [Fact]
        public void GeneratedChartSelectionSynchronizer_Maps_Stable_Keys_In_Both_Directions()
        {
            var items = new ObservableCollection<SampleRow>
            {
                new() { Id = 10, Category = "A" },
                new() { Id = 20, Category = "B" },
                new() { Id = 30, Category = "C" }
            };
            using var keyMap = new DataGridGeneratedListChartKeyMap<SampleRow, int>(items, SampleRowKey.Instance);
            var selection = new DataGridGeneratedSelectionController<SampleRow, int>(SampleRowKey.Instance);
            selection.ResetSource(items);
            var interaction = new ChartInteractionState();
            using var synchronizer = new DataGridGeneratedChartSelectionSynchronizer<SampleRow, int>(
                keyMap,
                selection,
                interaction,
                categoryToSourceIndex: static categoryIndex => categoryIndex + 1,
                sourceToCategoryIndex: static sourceIndex => sourceIndex - 1,
                categoryLabel: index => items[index + 1].Category);

            selection.SelectOnlyKey(30);
            Assert.Equal(1, interaction.CrosshairCategoryIndex);
            Assert.Equal("C", interaction.CrosshairCategoryLabel);

            interaction.SetCrosshair(0, "B", null, 0.5d, 0.5d);
            Assert.Equal(new[] { 20 }, selection.SelectedItemKeys);

            selection.SelectOnlyKey(10);
            Assert.False(interaction.IsCrosshairVisible);
        }

        [Fact]
        public void GeneratedLongFormChartSource_Partitions_And_Aggregates_With_Typed_Selectors()
        {
            var fields = new IDataGridGeneratedAnalyticsField[]
            {
                new DataGridGeneratedAnalyticsField<SampleRow, string>(
                    "category", DataGridGeneratedAnalyticsRole.ChartCategory, 0, static row => row.Category),
                new DataGridGeneratedAnalyticsField<SampleRow, string>(
                    "region", DataGridGeneratedAnalyticsRole.ChartSeries, 0, static row => row.Region),
                new DataGridGeneratedAnalyticsField<SampleRow, double>(
                    "revenue", DataGridGeneratedAnalyticsRole.ChartValue, 0, static row => row.Revenue,
                    numericValueSelector: static item => item is SampleRow row ? row.Revenue : null,
                    name: "Revenue", aggregate: (int)DataGridAggregateType.Sum)
            };
            var items = new ObservableCollection<SampleRow>
            {
                new() { Category = "A", Region = "North", Revenue = 10d },
                new() { Category = "A", Region = "South", Revenue = 20d },
                new() { Category = "A", Region = "North", Revenue = 5d },
                new() { Category = "B", Region = "North", Revenue = 30d },
                new() { Category = "B", Region = "South", Revenue = 40d }
            };
            using DataGridGeneratedLongFormChartDataSource source = DataGridGeneratedChartAdapter.CreateLongFormSource(
                items, fields, maximumItems: 16, maximumSeries: 4);
            using var model = new ChartModel { DataSource = source };

            Assert.Equal(new[] { "A", "B" }, model.Snapshot.Categories);
            Assert.Equal(new[] { "North", "South" }, model.Snapshot.Series.Select(static series => series.Name));
            Assert.Equal(new double?[] { 15d, 30d }, model.Snapshot.Series[0].Values);
            Assert.Equal(new double?[] { 20d, 40d }, model.Snapshot.Series[1].Values);

            items.Add(new SampleRow { Category = "C", Region = "North", Revenue = 50d });
            Assert.Equal(new[] { "A", "B", "C" }, model.Snapshot.Categories);
            Assert.Equal(50d, model.Snapshot.Series[0].Values[2]);

            model.Request.WindowStart = 1;
            model.Request.WindowCount = 2;
            Assert.Equal(new[] { "B", "C" }, model.Snapshot.Categories);
            Assert.Equal(3, source.GetTotalCategoryCount());
        }

        [Fact]
        public void BuildSnapshot_Uses_Category_And_Value_Paths()
        {
            var items = new[]
            {
                new SampleRow { Category = "A", Revenue = 10d },
                new SampleRow { Category = "B", Revenue = 25d }
            };

            var model = new DataGridChartModel
            {
                ItemsSource = items,
                CategoryPath = nameof(SampleRow.Category)
            };
            model.Series.Add(new DataGridChartSeriesDefinition
            {
                Name = "Revenue",
                ValuePath = nameof(SampleRow.Revenue),
                Kind = ChartSeriesKind.Column
            });

            var snapshot = model.BuildSnapshot(new ChartDataRequest());

            Assert.Equal(2, snapshot.Categories.Count);
            Assert.Equal("A", snapshot.Categories[0]);
            Assert.Equal("B", snapshot.Categories[1]);
            Assert.Equal(10d, snapshot.Series[0].Values[0]);
            Assert.Equal(25d, snapshot.Series[0].Values[1]);
        }

        [Fact]
        public void BuildSnapshot_Evaluates_Formula_Series()
        {
            var items = new[]
            {
                new SampleRow { Category = "A", Revenue = 10d, Cost = 3d },
                new SampleRow { Category = "B", Revenue = 20d, Cost = 8d }
            };

            var model = new DataGridChartModel
            {
                ItemsSource = items,
                CategoryPath = nameof(SampleRow.Category)
            };
            model.Series.Add(new DataGridChartSeriesDefinition
            {
                Name = "Profit",
                Formula = "Revenue-Cost",
                Kind = ChartSeriesKind.Line
            });

            var snapshot = model.BuildSnapshot(new ChartDataRequest());

            Assert.Equal(7d, snapshot.Series[0].Values[0]);
            Assert.Equal(12d, snapshot.Series[0].Values[1]);
        }

        [Fact]
        public void BuildSnapshot_Evaluates_Structured_Reference_Formulas()
        {
            var items = new[]
            {
                new SampleRow { Category = "A", Revenue = 2d },
                new SampleRow { Category = "B", Revenue = 4d }
            };

            var model = new DataGridChartModel
            {
                ItemsSource = items,
                CategoryPath = nameof(SampleRow.Category)
            };
            model.Series.Add(new DataGridChartSeriesDefinition
            {
                Name = "Double",
                Formula = "[@Revenue]*2",
                Kind = ChartSeriesKind.Line
            });

            var snapshot = model.BuildSnapshot(new ChartDataRequest());

            Assert.Equal(4d, snapshot.Series[0].Values[0]);
            Assert.Equal(8d, snapshot.Series[0].Values[1]);
        }

        [Fact]
        public void TryBuildUpdate_Tracks_Insert_Changes()
        {
            var items = new ObservableCollection<SampleRow>
            {
                new SampleRow { Category = "A", Revenue = 1d },
                new SampleRow { Category = "B", Revenue = 2d }
            };

            var model = new DataGridChartModel
            {
                ItemsSource = items,
                CategoryPath = nameof(SampleRow.Category)
            };
            model.Series.Add(new DataGridChartSeriesDefinition
            {
                Name = "Revenue",
                ValuePath = nameof(SampleRow.Revenue),
                Kind = ChartSeriesKind.Column
            });

            var request = new ChartDataRequest();
            var snapshot = model.BuildSnapshot(request);

            items.Add(new SampleRow { Category = "C", Revenue = 3d });

            var updated = model.TryBuildUpdate(request, snapshot, out var update);

            Assert.True(updated);
            Assert.Equal(ChartDataDeltaKind.Insert, update.Delta.Kind);
            Assert.Equal(2, update.Delta.Index);
            Assert.Equal(3, update.Snapshot.Categories.Count);
            Assert.Equal(3d, update.Snapshot.Series[0].Values[2]);
        }

        private sealed class SampleRow
        {
            public int Id { get; set; }

            public string Category { get; set; } = string.Empty;

            public string Region { get; set; } = string.Empty;

            public double Revenue { get; set; }

            public double Cost { get; set; }
        }

        private sealed class SampleRowKey : IDataGridItemKey<SampleRow, int>
        {
            public static SampleRowKey Instance { get; } = new();

            public int GetKey(SampleRow item) => item.Id;
        }

        private static IDataGridGeneratedAnalyticsField[] CreateGeneratedFields() =>
            new IDataGridGeneratedAnalyticsField[]
            {
                new DataGridGeneratedAnalyticsField<SampleRow, string>(
                    "category", DataGridGeneratedAnalyticsRole.ChartCategory, 0, static row => row.Category),
                new DataGridGeneratedAnalyticsField<SampleRow, double>(
                    "revenue", DataGridGeneratedAnalyticsRole.ChartValue, 0, static row => row.Revenue,
                    numericValueSelector: static item => item is SampleRow row ? row.Revenue : null,
                    name: "Revenue"),
                new DataGridGeneratedAnalyticsField<SampleRow, double>(
                    "cost", DataGridGeneratedAnalyticsRole.ChartValue, 1, static row => row.Cost,
                    numericValueSelector: static item => item is SampleRow row ? row.Cost : null,
                    name: "Cost")
            };
    }
}
