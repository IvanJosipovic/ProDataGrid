// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;

namespace ProDataGrid.SourceGeneration.Benchmarks;

internal static class BenchmarkCorrectness
{
    internal static void Validate()
    {
        BenchmarkTradeRow row = BenchmarkData.CreateRows(1)[0];
        DataGridColumnDefinitionList handwritten = HandwrittenBenchmarkTradeSchema.CreateCompiledColumns();
        DataGridColumnDefinitionList generated = BenchmarkTradeSchema.Instance.CreateColumnDefinitions();
        DataGridColumnDefinitionList expressions = HandwrittenBenchmarkTradeSchema.CreateExpressionColumns();

        ValidateColumns(handwritten, generated, row);
        ValidateColumns(handwritten, expressions, row);

        if (!BenchmarkTradeSchema.Instance.CreateFastPathOptions().StrictMode)
        {
            throw new InvalidOperationException("The generated benchmark schema must use strict fast-path options.");
        }

        ValidateOperations();
        ValidateAnalytics();

        var generatorBenchmarks = new SourceGeneratorBenchmarks { SchemaCount = 4 };
        generatorBenchmarks.Validate();
    }

    private static void ValidateAnalytics()
    {
        var projection = new GeneratedChartRangeProjectionBenchmarks { RowCount = 256 };
        projection.Setup();
        if (projection.CreateBoundedGeneratedRange() != 258)
        {
            throw new InvalidOperationException("Generated analytics range projection returned unexpected output.");
        }
        if (projection.BuildGeneratedLongFormSeries() != 16)
        {
            throw new InvalidOperationException("Generated long-form analytics projection returned unexpected output.");
        }
        if (projection.BuildGeneratedOutline() != 3)
        {
            throw new InvalidOperationException("Generated outline analytics projection returned unexpected output.");
        }

        var identity = new GeneratedChartKeyLookupBenchmarks { RowCount = 256 };
        identity.Setup();
        try
        {
            if (identity.LinearSourceScan() != identity.GeneratedStableKeyIndex())
            {
                throw new InvalidOperationException("Generated analytics stable-key lookup differs from the source scan.");
            }
        }
        finally
        {
            identity.Cleanup();
        }
    }

    private static void ValidateColumns(
        DataGridColumnDefinitionList expected,
        DataGridColumnDefinitionList actual,
        BenchmarkTradeRow row)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException("Benchmark column providers must create the same number of columns.");
        }

        for (int index = 0; index < expected.Count; index++)
        {
            DataGridColumnDefinition expectedColumn = expected[index];
            DataGridColumnDefinition actualColumn = actual[index];
            if (!Equals(expectedColumn.ColumnKey, actualColumn.ColumnKey) ||
                !Equals(expectedColumn.Header, actualColumn.Header) ||
                !string.Equals(expectedColumn.SortMemberPath, actualColumn.SortMemberPath, StringComparison.Ordinal) ||
                expectedColumn.GetType() != actualColumn.GetType() ||
                expectedColumn.ValueType != actualColumn.ValueType ||
                expectedColumn.ValueAccessor.ItemType != actualColumn.ValueAccessor.ItemType ||
                expectedColumn.ValueAccessor.ValueType != actualColumn.ValueAccessor.ValueType ||
                expectedColumn.ValueAccessor.CanWrite != actualColumn.ValueAccessor.CanWrite ||
                !Equals(expectedColumn.ValueAccessor.GetValue(row), actualColumn.ValueAccessor.GetValue(row)))
            {
                throw new InvalidOperationException($"Benchmark column {index} is not semantically equivalent.");
            }

            ValidateWriteSemantics(expectedColumn, actualColumn, row, index);
        }
    }

    private static void ValidateWriteSemantics(
        DataGridColumnDefinition expected,
        DataGridColumnDefinition actual,
        BenchmarkTradeRow row,
        int index)
    {
        if (!expected.ValueAccessor.CanWrite)
        {
            return;
        }

        object original = expected.ValueAccessor.GetValue(row);
        object replacement = index switch
        {
            0 => 101,
            1 => "UPDATED",
            2 => "Test",
            3 => 123.45m,
            4 => 9001L,
            _ => throw new InvalidOperationException($"Unexpected benchmark column index {index}.")
        };

        expected.ValueAccessor.SetValue(row, replacement);
        object expectedValue = expected.ValueAccessor.GetValue(row);
        expected.ValueAccessor.SetValue(row, original);

        actual.ValueAccessor.SetValue(row, replacement);
        object actualValue = actual.ValueAccessor.GetValue(row);
        actual.ValueAccessor.SetValue(row, original);

        if (!Equals(expectedValue, actualValue))
        {
            throw new InvalidOperationException($"Benchmark column {index} write semantics differ.");
        }
    }

    private static void ValidateOperations()
    {
        BenchmarkTradeRow[] rows = BenchmarkData.CreateRows(64);
        var sorting = new SortingExecutionBenchmarks();
        sorting.Setup();
        if (sorting.HandwrittenCompiledFastPath() != sorting.GeneratedStrictPath())
        {
            throw new InvalidOperationException("Generated and handwritten sorting results differ.");
        }

        var filtering = new FilteringExecutionBenchmarks();
        filtering.Setup();
        if (filtering.HandwrittenCompiledFastPath() != filtering.GeneratedStrictPath())
        {
            throw new InvalidOperationException("Generated and handwritten filtering results differ.");
        }

        var searching = new SearchingExecutionBenchmarks();
        searching.Setup();
        if (searching.HandwrittenCompiledFastPath() != searching.GeneratedStrictPath())
        {
            throw new InvalidOperationException("Generated and handwritten searching results differ.");
        }

        decimal handwritten = 0;
        decimal generated = 0;
        for (int index = 0; index < rows.Length; index++)
        {
            handwritten += HandwrittenBenchmarkTradeSchema.PriceAccessor.GetValue(rows[index]);
            generated += BenchmarkTradeSchema.Price.TypedAccessor.GetValue(rows[index]);
        }
        if (handwritten != generated)
        {
            throw new InvalidOperationException("Generated and handwritten accessor results differ.");
        }
    }
}
