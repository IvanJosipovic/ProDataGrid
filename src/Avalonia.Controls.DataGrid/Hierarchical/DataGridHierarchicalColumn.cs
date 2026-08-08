// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Collections;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Column that renders hierarchical rows with an expander and indentation.
    /// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    class DataGridHierarchicalColumn : DataGridBoundColumn
    {
        private static readonly IValueConverter _isExpandableConverter =
            new FuncValueConverter<bool, bool>(value => !value);
        private static readonly Binding _dataContextBinding = new Binding { Mode = BindingMode.OneWay };

        private readonly Lazy<IDataTemplate?> _cellTemplate;
        private readonly Lazy<ControlTheme?> _directCellTheme;
        private bool _refreshingBinding;

        public DataGridHierarchicalColumn()
        {
            BindingTarget = ContentControl.ContentProperty;
            IsReadOnly = true;

            _cellTemplate = new Lazy<IDataTemplate?>(() =>
                OwningGrid != null && OwningGrid.TryFindResource("DataGridHierarchicalCellTemplate", out var template)
                    ? (IDataTemplate)template
                    : null);
            _directCellTheme = new Lazy<ControlTheme?>(() => GetColumnControlTheme("DataGridOptimizedDirectHierarchicalCellTheme"));
        }

        /// <summary>
        /// Defines the <see cref="UseDirectCell"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> UseDirectCellProperty =
            AvaloniaProperty.Register<DataGridHierarchicalColumn, bool>(nameof(UseDirectCell));

        /// <summary>
        /// Gets or sets whether the retained expander presenter is combined with its DataGrid cell container.
        /// </summary>
        public bool UseDirectCell
        {
            get => GetValue(UseDirectCellProperty);
            set => SetValue(UseDirectCellProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Indent"/> property.
        /// </summary>
        public static readonly DirectProperty<DataGridHierarchicalColumn, double> IndentProperty =
            AvaloniaProperty.RegisterDirect<DataGridHierarchicalColumn, double>(
                nameof(Indent),
                o => o.Indent,
                (o, v) => o.Indent = v,
                16d);

        private double _indent = 16d;

        /// <summary>
        /// Gets or sets the per-level indent applied to the presenter.
        /// </summary>
        public double Indent
        {
            get => _indent;
            set
            {
                if (Math.Abs(_indent - value) > double.Epsilon)
                {
                    _indent = value;
                    NotifyPropertyChanged(nameof(Indent));
                }
            }
        }

        /// <summary>
        /// Gets or sets the template used to display the cell content.
        /// </summary>
        public IDataTemplate? CellTemplate { get; set; }

        /// <inheritdoc />
        public override BindingBase Binding
        {
            get => base.Binding;
            set
            {
                _refreshingBinding = true;
                try
                {
                    base.Binding = value;
                }
                finally
                {
                    _refreshingBinding = false;
                }
            }
        }

        /// <inheritdoc />
        protected override Control GenerateElement(DataGridCell cell, object dataItem)
        {
            if (cell is DataGridDirectHierarchicalCell directCell)
            {
                directCell.Content = null;
                directCell.Theme = CellTheme ?? GetDirectCellTheme();
                directCell.Indent = Indent;
                BindContent(directCell, dataItem);
                return null;
            }

            if (cell.Content is DataGridHierarchicalPresenter existingPresenter && !_refreshingBinding)
            {
                BindContent(existingPresenter, dataItem, isEditing: false);
                return existingPresenter;
            }

            var presenter = cell.Content as DataGridHierarchicalPresenter ?? CreatePresenter();
            BindContent(presenter, dataItem, isEditing: false);
            return presenter;
        }

        /// <inheritdoc />
        protected override Control GenerateEditingElementDirect(DataGridCell cell, object dataItem)
        {
            var presenter = CreatePresenter();
            BindContent(presenter, dataItem, isEditing: true);
            return presenter;
        }

        /// <inheritdoc />
        protected internal override void RefreshCellContent(Control element, string propertyName)
        {
            base.RefreshCellContent(element, propertyName);

            if (propertyName == nameof(Indent) && element is DataGridHierarchicalPresenter presenter)
            {
                presenter.Indent = Indent;
            }
        }

        /// <inheritdoc />
        protected override object? PrepareCellForEdit(Control editingElement, Avalonia.Interactivity.RoutedEventArgs editingEventArgs)
        {
            return (editingElement as ContentControl)?.Content;
        }

        private DataGridHierarchicalPresenter CreatePresenter()
        {
            var presenter = new DataGridHierarchicalPresenter
            {
                Indent = Indent
            };

            presenter.ToggleRequested += PresenterOnToggleRequested;
            presenter.Bind(
                DataGridHierarchicalPresenter.LevelProperty,
                new Binding(nameof(HierarchicalNode.Level)) { Mode = BindingMode.OneWay });
            presenter.Bind(
                DataGridHierarchicalPresenter.IsExpandedProperty,
                new Binding(nameof(HierarchicalNode.IsExpanded)) { Mode = BindingMode.OneWay });
            presenter.Bind(
                DataGridHierarchicalPresenter.IsExpandableProperty,
                new Binding(nameof(HierarchicalNode.IsLeaf))
                {
                    Mode = BindingMode.OneWay,
                    Converter = _isExpandableConverter
                });

            return presenter;
        }

        internal override DataGridCell CreateCell()
        {
            if (!UseDirectCell)
            {
                return base.CreateCell();
            }

            var cell = new DataGridDirectHierarchicalCell();
            cell.ToggleRequested += PresenterOnToggleRequested;
            return cell;
        }

        internal override ControlTheme ResolveCellTheme(DataGrid grid)
        {
            return UseDirectCell
                ? CellTheme ?? GetDirectCellTheme() ?? base.ResolveCellTheme(grid)
                : base.ResolveCellTheme(grid);
        }

        private ControlTheme? GetDirectCellTheme()
        {
            return _directCellTheme.IsValueCreated
                ? _directCellTheme.Value
                : OwningGrid == null ? null : _directCellTheme.Value;
        }

        private void PresenterOnToggleRequested(object? sender, EventArgs e)
        {
            if (OwningGrid?.HierarchicalModel == null)
            {
                return;
            }

            if (sender is Control presenter && presenter.DataContext is HierarchicalNode node)
            {
                var row = presenter.FindAncestorOfType<DataGridRow>();
                if (row != null)
                {
                    OwningGrid.PrepareHierarchicalAnchor(row.Slot);
                }

                OwningGrid.HierarchicalModel.Toggle(node);
            }
        }

        private void BindContent(DataGridDirectHierarchicalCell cell, object dataItem)
        {
            if (Binding != null && dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                cell.Bind(ContentControl.ContentProperty, Binding);
            }
            else if (dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                cell.Bind(ContentControl.ContentProperty, _dataContextBinding);
            }
            else
            {
                cell.Content = dataItem;
            }

            cell.ContentTemplate = CellTemplate ?? _cellTemplate.Value;
        }

        private void BindContent(DataGridHierarchicalPresenter presenter, object dataItem, bool isEditing)
        {
            if (Binding != null && dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                presenter.Bind(ContentControl.ContentProperty, Binding);
            }
            else if (dataItem != DataGridCollectionView.NewItemPlaceholder)
            {
                presenter.Bind(ContentControl.ContentProperty, _dataContextBinding);
            }
            else
            {
                presenter.Content = dataItem;
            }

            presenter.ContentTemplate = CellTemplate ?? _cellTemplate.Value;
        }
    }
}
