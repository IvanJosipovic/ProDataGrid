// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia;
using Avalonia.Controls.DataGridDragDrop;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    partial class DataGrid
    {
        public static readonly RoutedEvent<DataGridRowDragStartingEventArgs> RowDragStartingEvent =
            RoutedEvent.Register<DataGrid, DataGridRowDragStartingEventArgs>(
                nameof(RowDragStarting),
                RoutingStrategies.Bubble);

        public static readonly RoutedEvent<DataGridRowDragCompletedEventArgs> RowDragCompletedEvent =
            RoutedEvent.Register<DataGrid, DataGridRowDragCompletedEventArgs>(
                nameof(RowDragCompleted),
                RoutingStrategies.Bubble);

        public event EventHandler<DataGridRowDragStartingEventArgs>? RowDragStarting
        {
            add => AddHandler(RowDragStartingEvent, value);
            remove => RemoveHandler(RowDragStartingEvent, value);
        }

        public event EventHandler<DataGridRowDragCompletedEventArgs>? RowDragCompleted
        {
            add => AddHandler(RowDragCompletedEvent, value);
            remove => RemoveHandler(RowDragCompletedEvent, value);
        }

        internal void OnRowDragStarting(DataGridRowDragStartingEventArgs e)
        {
            e.RoutedEvent ??= RowDragStartingEvent;
            e.Source ??= this;
            RaiseEvent(e);
        }

        internal void OnRowDragCompleted(DataGridRowDragCompletedEventArgs e)
        {
            e.RoutedEvent ??= RowDragCompletedEvent;
            e.Source ??= this;
            RaiseEvent(e);
        }

        private void RefreshRowDragDropController()
        {
            _rowDragDropController?.Dispose();
            _rowDragDropController = null;

            if (!CanUserReorderRows)
            {
                UpdatePseudoClasses();
                return;
            }

            var handler = RowDropHandler
                ?? _rowDropHandler
                ?? (_hierarchicalRowsEnabled
                    ? new DataGridHierarchicalRowReorderHandler()
                    : new DataGridRowReorderHandler());
            _rowDropHandler = handler;

            var options = _rowDragDropOptions ?? new DataGridRowDragDropOptions();
            _rowDragDropOptions = options;

            var factory = RowDragDropControllerFactory ?? _rowDragDropControllerFactory;
            _rowDragDropController = factory?.Create(this, handler, options)
                ?? new DataGridRowDragDropController(this, handler, options);

            UpdatePseudoClasses();
        }

        private void OnCanUserReorderRowsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_areHandlersSuspended)
            {
                return;
            }

            RefreshRowDragDropController();
        }

        private void OnRowDragHandleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_areHandlersSuspended)
            {
                return;
            }

            UpdatePseudoClasses();
        }

        private void OnRowDragHandleVisibleChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_areHandlersSuspended)
            {
                return;
            }

            UpdatePseudoClasses();
        }

        private void OnRowDropHandlerChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_areHandlersSuspended)
            {
                return;
            }

            _rowDropHandler = e.NewValue as IDataGridRowDropHandler
                ?? (_hierarchicalRowsEnabled
                    ? new DataGridHierarchicalRowReorderHandler()
                    : new DataGridRowReorderHandler());
            RefreshRowDragDropController();
        }

        private void OnRowDragDropOptionsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_areHandlersSuspended)
            {
                return;
            }

            RefreshRowDragDropController();
        }

        private void OnRowDragDropControllerFactoryChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_areHandlersSuspended)
            {
                return;
            }

            _rowDragDropControllerFactory = e.NewValue as IDataGridRowDragDropControllerFactory;
            RefreshRowDragDropController();
        }
    }
}
