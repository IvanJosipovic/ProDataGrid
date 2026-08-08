// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Avalonia.Controls.DataGridTests.ColumnDefinitions;

public sealed class DataGridGeneratedCollectionMutationTests
{
    [Fact]
    public async Task Service_forwards_each_range_as_one_domain_operation()
    {
        var handler = new RecordingHandler();
        var service = new DataGridGeneratedCollectionMutationService<int>(handler, maximumItemsPerMutation: 4);

        await service.AddAsync(1, new[] { 2, 3 });
        await service.RemoveAsync(2, new[] { 4, 5 });
        await service.ReplaceAsync(3, new[] { 6 }, new[] { 7, 8 });
        await service.MoveAsync(4, 9, 2);
        await service.ResetAsync(new[] { 10, 11, 12 });

        Assert.Equal(
            ["add:1:2,3", "remove:2:4,5", "replace:3:6:7,8", "move:4:9:2", "reset:10,11,12"],
            handler.Operations);
        Assert.Equal(4, service.MaximumItemsPerMutation);
    }

    [Fact]
    public async Task Service_rejects_invalid_or_oversized_mutations_before_handler()
    {
        var handler = new RecordingHandler();
        var service = new DataGridGeneratedCollectionMutationService<int>(handler, maximumItemsPerMutation: 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DataGridGeneratedCollectionMutationService<int>(handler, 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.AddAsync(-1, new[] { 1 }));
        await Assert.ThrowsAsync<ArgumentException>(async () => await service.RemoveAsync(0, ReadOnlyMemory<int>.Empty));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.ResetAsync(new[] { 1, 2, 3 }));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await service.MoveAsync(0, 1, 0));

        Assert.Empty(handler.Operations);
    }

    [Fact]
    public async Task Services_observe_pre_cancelled_tokens_and_new_row_policy()
    {
        var handler = new RecordingHandler();
        var mutations = new DataGridGeneratedCollectionMutationService<int>(handler);
        var rows = new DataGridGeneratedNewRowService<int>(new ConstantRowFactory());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await mutations.AddAsync(0, new[] { 1 }, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await rows.CreateAsync(cancellation.Token));
        Assert.Equal(42, await rows.CreateAsync());
        Assert.Empty(handler.Operations);
    }

    private sealed class ConstantRowFactory : IDataGridGeneratedNewRowFactory<int>
    {
        public ValueTask<int> CreateAsync(CancellationToken cancellationToken) => ValueTask.FromResult(42);
    }

    private sealed class RecordingHandler : IDataGridGeneratedCollectionMutationHandler<int>
    {
        public List<string> Operations { get; } = [];

        public ValueTask AddAsync(int index, ReadOnlyMemory<int> items, CancellationToken cancellationToken)
        {
            Operations.Add($"add:{index}:{Join(items)}");
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(int index, ReadOnlyMemory<int> items, CancellationToken cancellationToken)
        {
            Operations.Add($"remove:{index}:{Join(items)}");
            return ValueTask.CompletedTask;
        }

        public ValueTask ReplaceAsync(
            int index,
            ReadOnlyMemory<int> oldItems,
            ReadOnlyMemory<int> newItems,
            CancellationToken cancellationToken)
        {
            Operations.Add($"replace:{index}:{Join(oldItems)}:{Join(newItems)}");
            return ValueTask.CompletedTask;
        }

        public ValueTask MoveAsync(int oldIndex, int newIndex, int count, CancellationToken cancellationToken)
        {
            Operations.Add($"move:{oldIndex}:{newIndex}:{count}");
            return ValueTask.CompletedTask;
        }

        public ValueTask ResetAsync(ReadOnlyMemory<int> items, CancellationToken cancellationToken)
        {
            Operations.Add($"reset:{Join(items)}");
            return ValueTask.CompletedTask;
        }

        private static string Join(ReadOnlyMemory<int> items) => string.Join(",", items.ToArray());
    }
}
