// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System.Collections.Generic;

namespace Avalonia.Controls.DataGridHierarchical
{
    #if !DATAGRID_INTERNAL
    public
    #else
    internal
    #endif
    interface IHierarchicalStateProvider
    {
        IReadOnlyCollection<object> CaptureExpandedState();

        void RestoreExpandedState(IEnumerable<object> keys);

        ExpandedStateKeyMode ExpandedStateKeyMode { get; }
    }

    #if !DATAGRID_INTERNAL
    public
    #else
    internal
    #endif
    interface IHierarchicalStateProviderWithKeyMode : IHierarchicalStateProvider
    {
        void RestoreExpandedState(IEnumerable<object> keys, ExpandedStateKeyMode keyMode);
    }
}
