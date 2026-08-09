// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using Avalonia.Interactivity;
using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Selection origin scoping helpers.
    /// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    partial class DataGrid
    {
        private DataGridSelectionChangeSource _pendingSelectionChangeSource = DataGridSelectionChangeSource.Unknown;
        private RoutedEventArgs _pendingSelectionTriggerEvent;
        private DataGridSelectionChangingGuarantee _pendingSelectionChangingGuarantee =
            DataGridSelectionChangingGuarantee.AtomicPreflight;

        /// <summary>
        /// Begins a selection change scope that captures the origin information until disposed.
        /// </summary>
        internal IDisposable BeginSelectionChangeScope(
            DataGridSelectionChangeSource source,
            RoutedEventArgs triggerEvent = null,
            bool sticky = false,
            DataGridSelectionChangingGuarantee guarantee = DataGridSelectionChangingGuarantee.AtomicPreflight)
        {
            var previousSource = _pendingSelectionChangeSource;
            var previousTrigger = _pendingSelectionTriggerEvent;
            var previousGuarantee = _pendingSelectionChangingGuarantee;

            _pendingSelectionChangeSource |= source;
            if (guarantee == DataGridSelectionChangingGuarantee.PostChangeReconciliation)
            {
                _pendingSelectionChangingGuarantee = guarantee;
            }
            else if ((previousSource & DataGridSelectionChangeSource.ItemsSourceChange) != 0 &&
                     (previousSource & ~DataGridSelectionChangeSource.ItemsSourceChange) == 0 &&
                     (source & ~DataGridSelectionChangeSource.ItemsSourceChange) != 0)
            {
                // ItemsSource reconciliation is sticky so delayed notifications retain their
                // origin. A later grid-controlled action starts a new atomic proposal and must
                // not inherit that completed reconciliation's weaker guarantee.
                _pendingSelectionChangingGuarantee = guarantee;
            }

            if (triggerEvent != null && _pendingSelectionTriggerEvent == null)
            {
                _pendingSelectionTriggerEvent = triggerEvent;
            }

            return new SelectionChangeScope(
                this,
                previousSource,
                previousTrigger,
                previousGuarantee,
                sticky);
        }

        internal DataGridSelectionChangeSource CurrentSelectionChangeSource => _pendingSelectionChangeSource;

        internal RoutedEventArgs CurrentSelectionTriggerEvent => _pendingSelectionTriggerEvent;

        internal DataGridSelectionChangingGuarantee CurrentSelectionChangingGuarantee =>
            _pendingSelectionChangingGuarantee;

        private void RestoreSelectionChangeScope(
            DataGridSelectionChangeSource source,
            RoutedEventArgs triggerEvent,
            DataGridSelectionChangingGuarantee guarantee)
        {
            _pendingSelectionChangeSource = source;
            _pendingSelectionTriggerEvent = triggerEvent;
            _pendingSelectionChangingGuarantee = guarantee;
        }

        private sealed class SelectionChangeScope : IDisposable
        {
            private DataGrid _owner;
            private readonly DataGridSelectionChangeSource _source;
            private readonly RoutedEventArgs _triggerEvent;
            private readonly DataGridSelectionChangingGuarantee _guarantee;
            private readonly bool _sticky;

            public SelectionChangeScope(
                DataGrid owner,
                DataGridSelectionChangeSource source,
                RoutedEventArgs triggerEvent,
                DataGridSelectionChangingGuarantee guarantee,
                bool sticky)
            {
                _owner = owner;
                _source = source;
                _triggerEvent = triggerEvent;
                _guarantee = guarantee;
                _sticky = sticky;
            }

            public void Dispose()
            {
                if (_owner != null && !_sticky)
                {
                    _owner.RestoreSelectionChangeScope(_source, _triggerEvent, _guarantee);
                    _owner = null;
                }
            }
        }
    }
}
