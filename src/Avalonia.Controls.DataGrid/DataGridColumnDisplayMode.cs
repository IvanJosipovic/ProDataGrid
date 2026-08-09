// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

namespace Avalonia.Controls
{
    /// <summary>
    /// Selects how a supported column realizes its display cells.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    enum DataGridColumnDisplayMode
    {
        /// <summary>
        /// Uses the column's normal retained display control and templates.
        /// </summary>
        Retained,

        /// <summary>
        /// Uses a lightweight drawing cell when the column supports drawing its current configuration.
        /// Unsupported configurations fall back to <see cref="Retained"/>.
        /// </summary>
        Drawn
    }

    abstract partial class DataGridColumn
    {
        /// <summary>
        /// Defines the <see cref="DisplayMode"/> property.
        /// </summary>
        public static readonly StyledProperty<DataGridColumnDisplayMode> DisplayModeProperty =
            AvaloniaProperty.Register<DataGridColumn, DataGridColumnDisplayMode>(nameof(DisplayMode));

        /// <summary>
        /// Gets or sets the requested display-cell realization mode. Retained mode is the compatibility default.
        /// </summary>
        public DataGridColumnDisplayMode DisplayMode
        {
            get => GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }

        internal virtual bool SupportsDrawnDisplay => false;

        internal bool UsesDrawnDisplay =>
            DisplayMode == DataGridColumnDisplayMode.Drawn && SupportsDrawnDisplay;
    }
}
