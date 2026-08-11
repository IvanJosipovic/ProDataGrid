// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Controls.DataGridTests.ColumnDefinitions;

public sealed class DataGridGeneratedEditingTests
{
    [Fact]
    public void Typed_field_parses_coerces_validates_and_formats_without_reflection()
    {
        Row row = new(1, 5m);
        DataGridGeneratedEditField<Row, decimal> field = CreateAmountField(
            validator: static (_, value) => value > 100m ? "too large" : null,
            coerce: static (_, value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero));

        DataGridGeneratedEditResult applied = field.TrySetText(
            row,
            "12.345".AsSpan(),
            CultureInfo.InvariantCulture,
            out object oldValue,
            out object newValue);
        DataGridGeneratedEditResult rejected = field.TrySetText(
            row,
            "101".AsSpan(),
            CultureInfo.InvariantCulture,
            out _,
            out _);

        Assert.Equal(DataGridGeneratedEditStatus.Applied, applied.Status);
        Assert.Equal(5m, oldValue);
        Assert.Equal(12.35m, newValue);
        Assert.Equal(12.35m, row.Amount);
        Assert.Equal("12.35", field.FormatValue(row, CultureInfo.InvariantCulture));
        Assert.Equal(DataGridGeneratedEditStatus.ValidationFailed, rejected.Status);
        Assert.Equal("too large", rejected.Error);
    }

    [Fact]
    public void Controller_groups_edits_into_keyed_undo_and_redo_batches()
    {
        Row first = new(1, 1m);
        Row second = new(2, 2m);
        var rows = new Dictionary<int, Row> { [1] = first, [2] = second };
        using var controller = new DataGridGeneratedEditController<Row, int>(
            new RowKey(),
            new IDataGridGeneratedEditField<Row>[] { CreateAmountField() },
            key => rows[key]);

        controller.BeginBatch();
        Assert.True(controller.TrySetText(first, "amount", "10".AsSpan(), CultureInfo.InvariantCulture).IsApplied);
        Assert.True(controller.TrySetValue(second, "amount", 20m).IsApplied);
        controller.CommitBatch();
        Assert.Equal((10m, 20m), (first.Amount, second.Amount));

        Assert.True(controller.Undo());
        Assert.Equal((1m, 2m), (first.Amount, second.Amount));
        Assert.True(controller.Redo());
        Assert.Equal((10m, 20m), (first.Amount, second.Amount));
    }

    [Fact]
    public void Eligibility_and_parse_failures_do_not_create_undo_records()
    {
        Row row = new(1, 1m) { Locked = true };
        DataGridGeneratedEditField<Row, decimal> field = CreateAmountField(canEdit: static item => !item.Locked);
        using var controller = new DataGridGeneratedEditController<Row, int>(
            new RowKey(),
            new IDataGridGeneratedEditField<Row>[] { field });

        Assert.Equal(
            DataGridGeneratedEditStatus.NotEditable,
            controller.TrySetValue(row, "amount", 2m).Status);
        Assert.Equal(
            DataGridGeneratedEditStatus.ParseFailed,
            controller.TrySetText(row, "amount", "not-number".AsSpan(), CultureInfo.InvariantCulture).Status);
        Assert.False(controller.CanUndo);
        Assert.Equal(1m, row.Amount);
    }

    [Fact]
    public async Task Async_validation_is_cancellable_revisioned_and_latest_result_wins()
    {
        Row row = new(1, 1m);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int invocation = 0;
        DataGridGeneratedEditField<Row, decimal> field = CreateAmountField(
            asyncValidator: async (_, _, cancellationToken) =>
            {
                int current = Interlocked.Increment(ref invocation);
                if (current == 1)
                {
                    firstStarted.SetResult();
                    await releaseFirst.Task.WaitAsync(cancellationToken);
                }
                return null;
            });
        using var controller = new DataGridGeneratedEditController<Row, int>(
            new RowKey(),
            new IDataGridGeneratedEditField<Row>[] { field });

        ValueTask<DataGridGeneratedEditResult> first = controller.TrySetValueAsync(
            row,
            "amount",
            2m,
            TestContext.Current.CancellationToken);
        await firstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        DataGridGeneratedEditResult second = await controller.TrySetValueAsync(
            row,
            "amount",
            3m,
            TestContext.Current.CancellationToken);
        releaseFirst.TrySetResult();
        DataGridGeneratedEditResult superseded = await first;

        Assert.Equal(DataGridGeneratedEditStatus.Applied, second.Status);
        Assert.Equal(DataGridGeneratedEditStatus.Superseded, superseded.Status);
        Assert.Equal(3m, row.Amount);
    }

    [Fact]
    public void Validation_projection_exposes_keyed_notify_and_observable_errors()
    {
        Row row = new(7, 5m);
        DataGridGeneratedEditField<Row, decimal> field = CreateAmountField(
            validator: static (_, value) => value > 100m ? "too large" : null);
        using var controller = new DataGridGeneratedEditController<Row, int>(
            new RowKey(),
            new IDataGridGeneratedEditField<Row>[] { field });
        using var projection = new DataGridGeneratedValidationProjection<Row, int>(
            new RowKey(),
            controller);
        var observer = new RecordingObserver<DataGridGeneratedValidationChange<int>>();
        using IDisposable subscription = projection.Subscribe(observer);
        string? changedProperty = null;
        projection.ErrorsChanged += (_, args) => changedProperty = args.PropertyName;

        DataGridGeneratedEditResult rejected = projection.TrySetValue(row, "amount", 101m);

        Assert.Equal(DataGridGeneratedEditStatus.ValidationFailed, rejected.Status);
        Assert.True(projection.HasErrors);
        Assert.Equal("too large", projection.GetError(7, "amount"));
        Assert.Equal("Amount", changedProperty);
        Assert.Equal(["too large"], projection.GetErrors("Amount").Cast<string>());
        DataGridGeneratedValidationChange<int> change = Assert.Single(observer.Values);
        Assert.True(change.HasError);
        Assert.Equal(
            new DataGridGeneratedValidationChange<int>(7, "amount", "Amount", rejected, hasError: true),
            change);

        DataGridGeneratedEditResult applied = projection.TrySetText(
            row,
            "amount",
            "12.5".AsSpan(),
            CultureInfo.InvariantCulture);

        Assert.True(applied.IsApplied);
        Assert.False(projection.HasErrors);
        Assert.Null(projection.GetError(7, "amount"));
        Assert.False(observer.Values[^1].HasError);
    }

    [Fact]
    public void Validation_projection_clears_keyed_errors_and_honors_controller_ownership()
    {
        Row first = new(1, 1m);
        Row second = new(2, 2m);
        DataGridGeneratedEditField<Row, decimal> field = CreateAmountField(
            validator: static (_, value) => value < 0m ? "negative" : null);
        var controller = new DataGridGeneratedEditController<Row, int>(
            new RowKey(),
            new IDataGridGeneratedEditField<Row>[] { field });
        var projection = new DataGridGeneratedValidationProjection<Row, int>(
            new RowKey(),
            controller,
            ownsController: true);
        var observer = new RecordingObserver<DataGridGeneratedValidationChange<int>>();
        using IDisposable subscription = projection.Subscribe(observer);

        projection.TrySetValue(first, "amount", -1m);
        projection.TrySetValue(second, "amount", -2m);
        Assert.True(projection.ClearError(1, "amount"));
        Assert.False(projection.ClearError(1, "amount"));
        Assert.Null(projection.GetError(1, "amount"));
        Assert.NotNull(projection.GetError(2, "amount"));

        projection.ClearErrors();
        Assert.False(projection.HasErrors);
        Assert.Empty(projection.GetErrors(null).Cast<string>());

        projection.Dispose();
        Assert.True(observer.Completed);
        Assert.Throws<ObjectDisposedException>(() => projection.TrySetValue(first, "amount", 1m));
        Assert.Throws<ObjectDisposedException>(() => controller.TrySetValue(first, "amount", 1m));
    }

    private static DataGridGeneratedEditField<Row, decimal> CreateAmountField(
        Func<Row, decimal, string?>? validator = null,
        Func<Row, decimal, CancellationToken, ValueTask<string?>>? asyncValidator = null,
        Func<Row, decimal, decimal>? coerce = null,
        Predicate<Row>? canEdit = null) =>
        new(
            "amount",
            static item => item.Amount,
            static (item, value) => item.Amount = value,
            static (ReadOnlySpan<char> text, IFormatProvider provider, out decimal value) =>
                decimal.TryParse(text, NumberStyles.Number, provider, out value),
            static (value, provider) => value.ToString("0.##", provider),
            validator,
            asyncValidator,
            coerce,
            canEdit,
            "Amount");

    private sealed class RecordingObserver<T> : IObserver<T>
    {
        public List<T> Values { get; } = new();

        public bool Completed { get; private set; }

        public void OnCompleted() => Completed = true;

        public void OnError(Exception error) => throw error;

        public void OnNext(T value) => Values.Add(value);
    }

    private sealed class Row
    {
        public Row(int id, decimal amount)
        {
            Id = id;
            Amount = amount;
        }

        public int Id { get; }
        public decimal Amount { get; set; }
        public bool Locked { get; set; }
    }

    private sealed class RowKey : IDataGridItemKey<Row, int>
    {
        public int GetKey(Row item) => item.Id;
    }
}
