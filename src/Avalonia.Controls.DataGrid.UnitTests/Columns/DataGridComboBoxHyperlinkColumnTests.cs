using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Xunit;

namespace Avalonia.Controls.DataGridTests.Columns;

public class DataGridComboBoxHyperlinkColumnTests
{
    [Fact]
    public void ComboBox_SortMemberPath_Comes_From_SelectedItemBinding()
    {
        var column = new DataGridComboBoxColumn
        {
            SelectedItemBinding = new Binding("Status")
        };

        Assert.Equal("Status", column.SortMemberPath);
    }

    [Fact]
    public void ComboBox_SortMemberPath_Is_Not_Overridden_When_Set()
    {
        var column = new DataGridComboBoxColumn
        {
            SortMemberPath = "Manual"
        };

        column.SelectedItemBinding = new Binding("Status");

        Assert.Equal("Manual", column.SortMemberPath);
    }

    [Fact]
    public void ComboBox_IsReadOnly_When_Binding_Is_OneWay()
    {
        var column = new DataGridComboBoxColumn
        {
            SelectedItemBinding = new Binding("Status") { Mode = BindingMode.OneWay }
        };

        Assert.True(column.IsReadOnly);
    }

    [Fact]
    public void ComboBox_PrepareCellForEdit_Opens_Dropdown_On_AltDown()
    {
        var column = new TestComboBoxColumn();
        var combo = new ComboBox { SelectedItem = "Original" };
        var args = new KeyEventArgs
        {
            Key = Key.Down,
            KeyModifiers = KeyModifiers.Alt
        };

        column.InvokePrepareCellForEdit(combo, args);

        Assert.True(combo.IsDropDownOpen);
    }

    [Fact]
    public void Hyperlink_ClipboardContentBinding_Prefers_ContentBinding()
    {
        var contentBinding = new Binding("Title");
        var column = new DataGridHyperlinkColumn
        {
            Binding = new Binding("Url"),
            ContentBinding = contentBinding
        };

        Assert.Same(column.ContentBinding, column.ClipboardContentBinding);
    }

    [Fact]
    public void Hyperlink_TargetName_Refreshes_Element()
    {
        var column = new DataGridHyperlinkColumn
        {
            TargetName = "_blank"
        };

        var hyperlink = new HyperlinkButton();
        column.RefreshCellContent(hyperlink, nameof(DataGridHyperlinkColumn.TargetName));

        Assert.Equal("_blank", hyperlink.Name);

        column.TargetName = null;
        column.RefreshCellContent(hyperlink, nameof(DataGridHyperlinkColumn.TargetName));

        Assert.True(string.IsNullOrEmpty(hyperlink.Name));
    }

    private sealed class TestComboBoxColumn : DataGridComboBoxColumn
    {
        public object InvokePrepareCellForEdit(Control editingElement, RoutedEventArgs args) =>
            base.PrepareCellForEdit(editingElement, args);
    }
}
