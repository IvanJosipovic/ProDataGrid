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

        return Owner is DataGridCustomDrawingCell drawingCell && drawingCell.Value != null
            ? Convert.ToString(drawingCell.Value, CultureInfo.CurrentCulture) ?? string.Empty
            : string.Empty;
    }
}
