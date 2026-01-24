// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia;
using Avalonia.Utilities;
using System.Collections.ObjectModel;

namespace Avalonia.Controls
{
    /// <summary>
    /// A collection of <see cref="DataGridSummaryDescription"/> objects.
    /// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    class DataGridSummaryDescriptionCollection : ObservableCollection<DataGridSummaryDescription>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridSummaryDescriptionCollection"/> class.
        /// </summary>
        public DataGridSummaryDescriptionCollection()
        {
        }

        /// <summary>
        /// Gets the owning column for this collection.
        /// </summary>
        internal DataGridColumn? OwningColumn { get; set; }

        protected override void InsertItem(int index, DataGridSummaryDescription item)
        {
            base.InsertItem(index, item);
            if (item != null)
            {
                WeakEventHandlerManager.Subscribe<AvaloniaObject, AvaloniaPropertyChangedEventArgs, DataGridSummaryDescriptionCollection>(
                    item,
                    nameof(AvaloniaObject.PropertyChanged),
                    OnSummaryDescriptionPropertyChanged);
            }
        }

        protected override void SetItem(int index, DataGridSummaryDescription item)
        {
            var oldItem = this[index];
            if (oldItem != null)
            {
                WeakEventHandlerManager.Unsubscribe<AvaloniaPropertyChangedEventArgs, DataGridSummaryDescriptionCollection>(
                    oldItem,
                    nameof(AvaloniaObject.PropertyChanged),
                    OnSummaryDescriptionPropertyChanged);
            }

            base.SetItem(index, item);

            if (item != null)
            {
                WeakEventHandlerManager.Subscribe<AvaloniaObject, AvaloniaPropertyChangedEventArgs, DataGridSummaryDescriptionCollection>(
                    item,
                    nameof(AvaloniaObject.PropertyChanged),
                    OnSummaryDescriptionPropertyChanged);
            }
        }

        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            if (oldItem != null)
            {
                WeakEventHandlerManager.Unsubscribe<AvaloniaPropertyChangedEventArgs, DataGridSummaryDescriptionCollection>(
                    oldItem,
                    nameof(AvaloniaObject.PropertyChanged),
                    OnSummaryDescriptionPropertyChanged);
            }

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                WeakEventHandlerManager.Unsubscribe<AvaloniaPropertyChangedEventArgs, DataGridSummaryDescriptionCollection>(
                    item,
                    nameof(AvaloniaObject.PropertyChanged),
                    OnSummaryDescriptionPropertyChanged);
            }

            base.ClearItems();
        }

        private void OnSummaryDescriptionPropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (OwningColumn != null)
            {
                OwningColumn.OwningGrid?.OnColumnSummariesChanged(OwningColumn);
            }
        }
    }
}
