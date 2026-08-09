// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using System;
using System.Collections;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Describes the origin of a selection change.
    /// </summary>
    [Flags]
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    enum DataGridSelectionChangeSource
    {
        Unknown = 0,
        Pointer = 1 << 0,
        Keyboard = 1 << 1,
        Command = 1 << 2,
        ItemsSourceChange = 1 << 3,
        Programmatic = 1 << 4,
        SelectionModelSync = 1 << 5,
        /// <summary>
        /// The selection was proposed by an active pointer drag interaction.
        /// This flag is combined with <see cref="Pointer"/>.
        /// </summary>
        DragInteraction = 1 << 6,
    }

    /// <summary>
    /// Provides additional context for DataGrid selection changes.
    /// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    class DataGridSelectionChangedEventArgs : SelectionChangedEventArgs
    {
        public DataGridSelectionChangedEventArgs(
            RoutedEvent routedEvent,
            IList removedItems,
            IList addedItems,
            DataGridSelectionChangeSource source = DataGridSelectionChangeSource.Unknown,
            RoutedEventArgs triggerEvent = null)
            : base(routedEvent, removedItems, addedItems)
        {
            Source = source;
            TriggerEvent = triggerEvent;
        }

        /// <summary>
        /// Gets a value indicating where the selection change originated.
        /// </summary>
        public new DataGridSelectionChangeSource Source { get; }

        /// <summary>
        /// Gets a value indicating whether the change was initiated by the user.
        /// </summary>
        public bool IsUserInitiated =>
            (Source & (DataGridSelectionChangeSource.Pointer |
                       DataGridSelectionChangeSource.Keyboard |
                       DataGridSelectionChangeSource.Command |
                       DataGridSelectionChangeSource.DragInteraction)) != 0;

        /// <summary>
        /// Gets the triggering routed event, when available.
        /// </summary>
        public RoutedEventArgs TriggerEvent { get; }
    }
}
