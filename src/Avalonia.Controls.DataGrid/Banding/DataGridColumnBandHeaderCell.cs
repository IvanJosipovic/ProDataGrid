// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

namespace Avalonia.Controls.DataGridBanding
{
    /// <summary>
    /// Presents a non-interactive grouped column-band header spanning one or more leaf columns.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridColumnBandHeaderCell : ContentControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridColumnBandHeaderCell"/> class.
        /// </summary>
        public DataGridColumnBandHeaderCell()
        {
            IsHitTestVisible = false;
        }

        internal int StartColumnIndex { get; set; }

        internal int EndColumnIndex { get; set; }

        internal int Level { get; set; }

        internal bool IsFrozenLeft { get; set; }

        internal bool IsFrozenRight { get; set; }
    }
}
