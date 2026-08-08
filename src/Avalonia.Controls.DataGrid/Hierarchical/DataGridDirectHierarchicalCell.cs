// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.ComponentModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
#if !DATAGRID_INTERNAL
    /// <summary>
    /// A retained hierarchical cell that combines the DataGrid cell and expander presenter roles.
    /// </summary>
    public
#else
    internal
#endif
    sealed class DataGridDirectHierarchicalCell : DataGridCell
    {
        private const string PartExpander = "PART_Expander";
        private HierarchicalNode? _node;
        private ToggleButton? _expander;

        /// <summary>Defines the <see cref="Level"/> property.</summary>
        public static readonly StyledProperty<int> LevelProperty =
            AvaloniaProperty.Register<DataGridDirectHierarchicalCell, int>(nameof(Level));

        /// <summary>Defines the <see cref="Indent"/> property.</summary>
        public static readonly StyledProperty<double> IndentProperty =
            AvaloniaProperty.Register<DataGridDirectHierarchicalCell, double>(nameof(Indent), 16d);

        /// <summary>Defines the <see cref="IsExpanded"/> property.</summary>
        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<DataGridDirectHierarchicalCell, bool>(nameof(IsExpanded));

        /// <summary>Defines the <see cref="IsExpandable"/> property.</summary>
        public static readonly StyledProperty<bool> IsExpandableProperty =
            AvaloniaProperty.Register<DataGridDirectHierarchicalCell, bool>(nameof(IsExpandable));

        /// <summary>Defines the <see cref="ToggleRequested"/> routed event.</summary>
        public static readonly RoutedEvent<RoutedEventArgs> ToggleRequestedEvent =
            RoutedEvent.Register<DataGridDirectHierarchicalCell, RoutedEventArgs>(nameof(ToggleRequested), RoutingStrategies.Bubble);

        /// <summary>Occurs when the cell's expander is activated.</summary>
        public event EventHandler<RoutedEventArgs>? ToggleRequested
        {
            add => AddHandler(ToggleRequestedEvent, value);
            remove => RemoveHandler(ToggleRequestedEvent, value);
        }

        /// <summary>Gets or sets the zero-based hierarchy level.</summary>
        public int Level
        {
            get => GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        /// <summary>Gets or sets the indentation applied for each hierarchy level.</summary>
        public double Indent
        {
            get => GetValue(IndentProperty);
            set => SetValue(IndentProperty, value);
        }

        /// <summary>Gets or sets whether the represented node is expanded.</summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>Gets or sets whether the represented node can be expanded.</summary>
        public bool IsExpandable
        {
            get => GetValue(IsExpandableProperty);
            set => SetValue(IsExpandableProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DataContextProperty)
            {
                AttachNode(change.NewValue as HierarchicalNode);
            }
            else if (change.Property == LevelProperty || change.Property == IndentProperty)
            {
                UpdateIndentPadding();
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            if (_expander != null)
            {
                _expander.Click -= ExpanderOnClick;
                _expander.KeyDown -= ExpanderOnKeyDown;
            }

            _expander = e.NameScope.Find<ToggleButton>(PartExpander);
            if (_expander != null)
            {
                _expander.Click += ExpanderOnClick;
                _expander.KeyDown += ExpanderOnKeyDown;
            }
        }

        private void AttachNode(HierarchicalNode? node)
        {
            if (ReferenceEquals(_node, node))
            {
                UpdateNodeState();
                return;
            }

            if (_node != null)
            {
                _node.PropertyChanged -= NodeOnPropertyChanged;
            }

            _node = node;
            if (_node != null)
            {
                _node.PropertyChanged += NodeOnPropertyChanged;
            }

            UpdateNodeState();
        }

        private void NodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(HierarchicalNode.Level) ||
                e.PropertyName == nameof(HierarchicalNode.IsExpanded) ||
                e.PropertyName == nameof(HierarchicalNode.IsLeaf))
            {
                UpdateNodeState();
            }
        }

        private void UpdateNodeState()
        {
            SetCurrentValue(LevelProperty, _node?.Level ?? 0);
            SetCurrentValue(IsExpandedProperty, _node?.IsExpanded == true);
            SetCurrentValue(IsExpandableProperty, _node?.IsLeaf == false);
            UpdateIndentPadding();
        }

        private void UpdateIndentPadding()
        {
            SetCurrentValue(PaddingProperty, new Thickness(Level * Math.Max(Indent, 0), 0, 0, 0));
        }

        private void ExpanderOnClick(object? sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent, this));
        }

        private void ExpanderOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (!e.Handled && (e.Key == Key.Enter || e.Key == Key.Space))
            {
                RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent, this));
                e.Handled = true;
            }
        }
    }
}
