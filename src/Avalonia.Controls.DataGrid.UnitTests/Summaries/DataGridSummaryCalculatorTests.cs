// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Generic;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Summaries;

/// <summary>
/// Tests for the summary calculator implementations via DataGridAggregateSummaryDescription.
/// </summary>
public class DataGridSummaryCalculatorTests
{
    #region Sum Calculator Tests

    [Fact]
    public void SumCalculator_Returns_Sum_Of_Integer_Values()
    {
        var items = new List<TestItem>
        {
            new() { Value = 10 },
            new() { Value = 20 },
            new() { Value = 30 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Sum
        };
        
        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(60m, result);
    }

    [Fact]
    public void SumCalculator_Returns_Sum_Of_Double_Values()
    {
        var items = new List<TestItem>
        {
            new() { DoubleValue = 10.5 },
            new() { DoubleValue = 20.25 },
            new() { DoubleValue = 30.75 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Sum
        };

        var column = CreateColumnWithBinding(nameof(TestItem.DoubleValue));
        var result = description.Calculate(items, column);

        Assert.Equal(61.5m, result);
    }

    [Fact]
    public void SumCalculator_Uses_ValueAccessor_When_No_Path()
    {
        var items = new List<TestItem>
        {
            new() { Value = 5 },
            new() { Value = 7 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Sum
        };

        var column = new DataGridTextColumn();
        DataGridColumnMetadata.SetValueAccessor(column, new DataGridColumnValueAccessor<TestItem, int>(x => x.Value));

        var result = description.Calculate(items, column);

        Assert.Equal(12m, result);
    }

    [Fact]
    public void SumCalculator_Returns_Null_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Sum
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Null(result);
    }

    [Fact]
    public void SumCalculator_Ignores_Null_Values()
    {
        var items = new List<TestItem>
        {
            new() { NullableValue = 10 },
            new() { NullableValue = null },
            new() { NullableValue = 30 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Sum
        };

        var column = CreateColumnWithBinding(nameof(TestItem.NullableValue));
        var result = description.Calculate(items, column);

        Assert.Equal(40m, result);
    }

    #endregion

    #region Average Calculator Tests

    [Fact]
    public void AverageCalculator_Returns_Average_Of_Values()
    {
        var items = new List<TestItem>
        {
            new() { Value = 10 },
            new() { Value = 20 },
            new() { Value = 30 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Average
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(20m, result);
    }

    [Fact]
    public void AverageCalculator_Returns_Null_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Average
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Null(result);
    }

    [Fact]
    public void AverageCalculator_Handles_Decimal_Results()
    {
        var items = new List<TestItem>
        {
            new() { Value = 10 },
            new() { Value = 20 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Average
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(15m, result);
    }

    #endregion

    #region Count Calculator Tests

    [Fact]
    public void CountCalculator_Returns_Total_Count()
    {
        var items = new List<TestItem>
        {
            new() { Value = 10 },
            new() { Value = 20 },
            new() { Value = 30 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Count
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(3, result);
    }

    [Fact]
    public void CountCalculator_Returns_Zero_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Count
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(0, result);
    }

    #endregion

    #region Count Distinct Calculator Tests

    [Fact]
    public void CountDistinctCalculator_Returns_Distinct_Count()
    {
        var items = new List<TestItem>
        {
            new() { Value = 10 },
            new() { Value = 20 },
            new() { Value = 10 },
            new() { Value = 30 },
            new() { Value = 20 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.CountDistinct
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(3, result);
    }

    [Fact]
    public void CountDistinctCalculator_Returns_Zero_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.CountDistinct
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(0, result);
    }

    #endregion

    #region Min Calculator Tests

    [Fact]
    public void MinCalculator_Returns_Minimum_Value()
    {
        var items = new List<TestItem>
        {
            new() { Value = 30 },
            new() { Value = 10 },
            new() { Value = 20 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Min
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(10, result);
    }

    [Fact]
    public void MinCalculator_Returns_Null_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Min
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Null(result);
    }

    [Fact]
    public void MinCalculator_Works_With_Strings()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie" },
            new() { Name = "Alice" },
            new() { Name = "Bob" }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Min
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Name));
        var result = description.Calculate(items, column);

        Assert.Equal("Alice", result);
    }

    #endregion

    #region Max Calculator Tests

    [Fact]
    public void MaxCalculator_Returns_Maximum_Value()
    {
        var items = new List<TestItem>
        {
            new() { Value = 30 },
            new() { Value = 10 },
            new() { Value = 20 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Max
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(30, result);
    }

    [Fact]
    public void MaxCalculator_Returns_Null_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Max
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Null(result);
    }

    [Fact]
    public void MaxCalculator_Works_With_Strings()
    {
        var items = new List<TestItem>
        {
            new() { Name = "Charlie" },
            new() { Name = "Alice" },
            new() { Name = "Bob" }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Max
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Name));
        var result = description.Calculate(items, column);

        Assert.Equal("Charlie", result);
    }

    #endregion

    #region First Calculator Tests

    [Fact]
    public void FirstCalculator_Returns_First_Value()
    {
        var items = new List<TestItem>
        {
            new() { Value = 10 },
            new() { Value = 20 },
            new() { Value = 30 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.First
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(10, result);
    }

    [Fact]
    public void FirstCalculator_Returns_Null_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.First
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Null(result);
    }

    #endregion

    #region Last Calculator Tests

    [Fact]
    public void LastCalculator_Returns_Last_Value()
    {
        var items = new List<TestItem>
        {
            new() { Value = 10 },
            new() { Value = 20 },
            new() { Value = 30 }
        };

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Last
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Equal(30, result);
    }

    [Fact]
    public void LastCalculator_Returns_Null_For_Empty_Collection()
    {
        var items = new List<TestItem>();

        var description = new DataGridAggregateSummaryDescription
        {
            Aggregate = DataGridAggregateType.Last
        };

        var column = CreateColumnWithBinding(nameof(TestItem.Value));
        var result = description.Calculate(items, column);

        Assert.Null(result);
    }

    #endregion

    #region Test Helpers

    private static DataGridTextColumn CreateColumnWithBinding(string propertyName)
    {
        return new DataGridTextColumn
        {
            Binding = new Avalonia.Data.Binding(propertyName)
        };
    }

    private class TestItem
    {
        public int Value { get; set; }
        public double DoubleValue { get; set; }
        public int? NullableValue { get; set; }
        public string Name { get; set; }
    }

    #endregion
}
