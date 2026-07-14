// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using Avalonia.Collections;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Utilities;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    class DataGridTemplateColumn : DataGridColumn
    {
        private IDataTemplate _cellTemplate;
        private bool _reuseCellContent;

        public static readonly DirectProperty<DataGridTemplateColumn, IDataTemplate> CellTemplateProperty =
            AvaloniaProperty.RegisterDirect<DataGridTemplateColumn, IDataTemplate>(
                nameof(CellTemplate),
                o => o.CellTemplate,
                (o, v) => o.CellTemplate = v);

        [Content]
        [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
        public IDataTemplate CellTemplate
        {
            get { return _cellTemplate; }
            set { SetAndRaise(CellTemplateProperty, ref _cellTemplate, value); }
        }

        /// <summary>
        /// Gets or sets whether existing cell content can be reused for recycled rows when the
        /// template does not support <see cref="IRecyclingDataTemplate"/>.
        /// </summary>
        public static readonly DirectProperty<DataGridTemplateColumn, bool> ReuseCellContentProperty =
            AvaloniaProperty.RegisterDirect<DataGridTemplateColumn, bool>(
                nameof(ReuseCellContent),
                o => o.ReuseCellContent,
                (o, v) => o.ReuseCellContent = v);

        /// <summary>
        /// When enabled, the column will keep the existing cell content control for recycled rows
        /// instead of rebuilding the template, as long as the template is not a recycling template.
        /// </summary>
        public bool ReuseCellContent
        {
            get => _reuseCellContent;
            set => SetAndRaise(ReuseCellContentProperty, ref _reuseCellContent, value);
        }

        private IDataTemplate _newRowCellTemplate;

        /// <summary>
        /// Defines the <see cref="NewRowCellTemplate"/> property.
        /// </summary>
        public static readonly DirectProperty<DataGridTemplateColumn, IDataTemplate> NewRowCellTemplateProperty =
            AvaloniaProperty.RegisterDirect<DataGridTemplateColumn, IDataTemplate>(
                nameof(NewRowCellTemplate),
                o => o.NewRowCellTemplate,
                (o, v) => o.NewRowCellTemplate = v);

        /// <summary>
        /// Gets or sets the template used for the placeholder row that allows adding a new item.
        /// </summary>
        [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
        public IDataTemplate NewRowCellTemplate
        {
            get => _newRowCellTemplate;
            set => SetAndRaise(NewRowCellTemplateProperty, ref _newRowCellTemplate, value);
        }

        private IDataTemplate _cellEditingCellTemplate;

        /// <summary>
        /// Defines the <see cref="CellEditingTemplate"/> property.
        /// </summary>
        public static readonly DirectProperty<DataGridTemplateColumn, IDataTemplate> CellEditingTemplateProperty =
                AvaloniaProperty.RegisterDirect<DataGridTemplateColumn, IDataTemplate>(
                    nameof(CellEditingTemplate),
                    o => o.CellEditingTemplate,
                    (o, v) => o.CellEditingTemplate = v);

        /// <summary>
        /// Gets or sets the <see cref="IDataTemplate"/> which is used for the editing mode of the current <see cref="DataGridCell"/>
        /// </summary>
        /// <value>
        /// An <see cref="IDataTemplate"/> for the editing mode of the current <see cref="DataGridCell"/>
        /// </value>
        /// <remarks>
        /// If this property is <see langword="null"/> the column is read-only.
        /// </remarks>
        [InheritDataTypeFromItems(nameof(DataGrid.ItemsSource), AncestorType = typeof(DataGrid))]
        public IDataTemplate CellEditingTemplate
        {
            get => _cellEditingCellTemplate;
            set => SetAndRaise(CellEditingTemplateProperty, ref _cellEditingCellTemplate, value);
        }

        private bool _forceGenerateCellFromTemplate;

        protected override void EndCellEdit()
        {
            // The next regular generation must replace the editing control with display content.
            _forceGenerateCellFromTemplate = true;
            base.EndCellEdit();
        }

        protected override Control GenerateElement(DataGridCell cell, object dataItem)
        {
            var forceNewContent = _forceGenerateCellFromTemplate;
            _forceGenerateCellFromTemplate = false;
            return GenerateElementCore(cell, dataItem, forceNewContent);
        }

        private Control GenerateElementCore(DataGridCell cell, object dataItem, bool forceNewContent)
        {
            Control recycledContent = forceNewContent ? null : cell.Content as Control;

            // A recycled row can briefly clear its DataContext while being re-templated; avoid invoking user templates with null.
            if (dataItem is null)
            {
                return recycledContent ?? new Control();
            }

            if (dataItem == DataGridCollectionView.NewItemPlaceholder)
            {
                if (NewRowCellTemplate != null)
                {
                    if (NewRowCellTemplate is IRecyclingDataTemplate recyclingNewRowTemplate)
                    {
                        return recyclingNewRowTemplate.Build(dataItem, recycledContent);
                    }

                    return NewRowCellTemplate.Build(dataItem);
                }

                return new Control();
            }

            if (CellTemplate != null)
            {
                if (ReuseCellContent &&
                    recycledContent != null &&
                    CellTemplate is not IRecyclingDataTemplate)
                {
                    return recycledContent;
                }
                return (CellTemplate is IRecyclingDataTemplate recyclingDataTemplate)
                    ? recyclingDataTemplate.Build(dataItem, recycledContent)
                    : CellTemplate.Build(dataItem);
            }
            if (Design.IsDesignMode)
            {
                return null;
            }
            else
            {
                throw DataGridError.DataGridTemplateColumn.MissingTemplateForType(typeof(DataGridTemplateColumn));
            }
        }

        protected override Control GenerateEditingElement(DataGridCell cell, object dataItem, out ICellEditBinding binding)
        {
            binding = null;
            if(CellEditingTemplate != null)
            {
                return CellEditingTemplate.Build(dataItem);
            }
            else if (CellTemplate != null)
            {
                return CellTemplate.Build(dataItem);
            }
            if (Design.IsDesignMode)
            {
                return null;
            }
            else
            {
                throw DataGridError.DataGridTemplateColumn.MissingTemplateForType(typeof(DataGridTemplateColumn));
            }
        }

        protected override object PrepareCellForEdit(Control editingElement, RoutedEventArgs editingEventArgs)
        {
            return null;
        }

        protected internal override void RefreshCellContent(Control element, string propertyName)
        {
            var cell = element?.Parent as DataGridCell;
            if(cell is not null && (propertyName == nameof(CellTemplate) || propertyName == nameof(NewRowCellTemplate)))
            {
                cell.Content = GenerateElementCore(cell, cell.DataContext, forceNewContent: true);
            }

            base.RefreshCellContent(element, propertyName);
        }

        internal void RefreshCellContentForDataContextChange(DataGridCell cell)
        {
            if (cell is null)
            {
                return;
            }

            cell.Content = GenerateElementCore(cell, cell.DataContext, forceNewContent: false);
        }
        
        public override bool IsReadOnly
        {
            get
            {
                if (CellEditingTemplate is null)
                {
                    return true;
                }

                return base.IsReadOnly;
            }
            set
            {
                base.IsReadOnly = value;
            }
        }
    }
}
