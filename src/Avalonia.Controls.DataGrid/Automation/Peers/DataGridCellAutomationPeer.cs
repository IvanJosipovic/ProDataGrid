using System;
using System.Globalization;
using Avalonia.Automation.Peers;

namespace Avalonia.Controls.Automation.Peers;

#if !DATAGRID_INTERNAL
public
#else
internal
#endif
class DataGridCellAutomationPeer : ContentControlAutomationPeer
{
    public DataGridCellAutomationPeer(DataGridCell owner)
        : base(owner)
    {
    }

    public new DataGridCell Owner => (DataGridCell)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Custom;
    }

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    protected override string GetNameCore()
    {
        var name = base.GetNameCore();
        if (!string.IsNullOrEmpty(name))
        {
            return name;
        }

        var value = Owner switch
        {
            DataGridCustomDrawingCell drawingCell => drawingCell.Value,
            DataGridDirectTextCell directTextCell => directTextCell.Value,
            DataGridDirectHierarchicalCell hierarchicalCell => hierarchicalCell.Value,
            _ => null
        };

        return value != null
            ? Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
            : string.Empty;
    }
}
