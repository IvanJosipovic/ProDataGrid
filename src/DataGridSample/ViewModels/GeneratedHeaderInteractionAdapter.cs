// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;

namespace DataGridSample.ViewModels;

internal sealed class GeneratedHeaderInteractionAdapter : IDataGridGeneratedHeaderInteraction, IDisposable
{
    private readonly Interaction<DataGridGeneratedHeaderCommandRequest, bool> _interaction;
    private readonly Action<DataGridGeneratedHeaderCommandRequest, bool, Exception?> _completed;
    private bool _disposed;

    public GeneratedHeaderInteractionAdapter(
        Interaction<DataGridGeneratedHeaderCommandRequest, bool> interaction,
        Action<DataGridGeneratedHeaderCommandRequest, bool, Exception?> completed)
    {
        _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
        _completed = completed ?? throw new ArgumentNullException(nameof(completed));
    }

    public Task LastExecution { get; private set; } = Task.CompletedTask;

    public bool CanExecute(DataGridGeneratedHeaderCommandRequest request) => !_disposed;

    public void Execute(DataGridGeneratedHeaderCommandRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        LastExecution = ExecuteAsync(request);
    }

    public void Dispose()
    {
        _disposed = true;
    }

    private async Task ExecuteAsync(DataGridGeneratedHeaderCommandRequest request)
    {
        try
        {
            bool handled = await _interaction.Handle(request).ToTask();
            if (!_disposed)
            {
                _completed(request, handled, null);
            }
        }
        catch (Exception exception)
        {
            if (!_disposed)
            {
                _completed(request, false, exception);
            }
        }
    }
}
