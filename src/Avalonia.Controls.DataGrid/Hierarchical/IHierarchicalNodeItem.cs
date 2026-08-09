// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

namespace Avalonia.Controls.DataGridHierarchical
{
    internal interface IHierarchicalNodeItem
    {
        object Item { get; }

        HierarchicalNode Node { get; }
    }
}
