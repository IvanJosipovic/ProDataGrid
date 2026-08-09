// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Presenter used by <see cref="DataGridHierarchicalColumn"/> to render an expander with indent.
    /// </summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    class DataGridHierarchicalPresenter : ContentControl
    {
        private const string PartExpander = "PART_Expander";
        private ToggleButton? _expander;
        private DataGridHierarchicalColumn? _directColumn;
        private HierarchicalNode? _node;
        private INotifyPropertyChanged? _itemNotifier;
        private WeakPropertyChangedListener<DataGridHierarchicalPresenter>? _nodePropertyChangedListener;
        private WeakPropertyChangedListener<DataGridHierarchicalPresenter>? _itemPropertyChangedListener;

        public static readonly StyledProperty<int> LevelProperty =
            AvaloniaProperty.Register<DataGridHierarchicalPresenter, int>(nameof(Level));

        public static readonly StyledProperty<double> IndentProperty =
            AvaloniaProperty.Register<DataGridHierarchicalPresenter, double>(nameof(Indent), 16d);

        public static readonly StyledProperty<bool> IsExpandedProperty =
            AvaloniaProperty.Register<DataGridHierarchicalPresenter, bool>(nameof(IsExpanded));

        public static readonly StyledProperty<bool> IsExpandableProperty =
            AvaloniaProperty.Register<DataGridHierarchicalPresenter, bool>(nameof(IsExpandable));

        /// <summary>
        /// Identifies the <see cref="ToggleRequested"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> ToggleRequestedEvent =
            RoutedEvent.Register<DataGridHierarchicalPresenter, RoutedEventArgs>(nameof(ToggleRequested), RoutingStrategies.Bubble);

        /// <summary>
        /// Raised when the expander is activated.
        /// </summary>
        public event EventHandler<RoutedEventArgs>? ToggleRequested
        {
            add => AddHandler(ToggleRequestedEvent, value);
            remove => RemoveHandler(ToggleRequestedEvent, value);
        }

        static DataGridHierarchicalPresenter()
        {
            FocusableProperty.OverrideDefaultValue<DataGridHierarchicalPresenter>(false);
        }

        public DataGridHierarchicalPresenter()
        {
            UpdateIndentPadding();
        }

        /// <summary>
        /// Gets or sets the level of the current node.
        /// </summary>
        public int Level
        {
            get => GetValue(LevelProperty);
            set => SetValue(LevelProperty, value);
        }

        /// <summary>
        /// Gets or sets the per-level indent.
        /// </summary>
        public double Indent
        {
            get => GetValue(IndentProperty);
            set => SetValue(IndentProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the node is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get => GetValue(IsExpandedProperty);
            set => SetValue(IsExpandedProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the node can be expanded.
        /// </summary>
        public bool IsExpandable
        {
            get => GetValue(IsExpandableProperty);
            set => SetValue(IsExpandableProperty, value);
        }

        internal bool UsesDirectValues => _directColumn != null;

        internal void ConfigureDirectValues(DataGridHierarchicalColumn column, object? dataItem)
        {
            DetachDirectSubscriptions();
            _directColumn = column;
            _node = dataItem switch
            {
                HierarchicalNode hierarchicalNode => hierarchicalNode,
                IHierarchicalNodeItem nodeItem => nodeItem.Node,
                _ => null,
            };

            UpdateDirectState();
            UpdateDirectContent();
            AttachDirectSubscriptions();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (_directColumn != null)
            {
                ConfigureDirectValues(_directColumn, DataContext);
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (_directColumn != null)
            {
                UpdateDirectState();
                UpdateDirectContent();
                AttachDirectSubscriptions();
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            DetachDirectSubscriptions();
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == LevelProperty ||
                change.Property == IndentProperty ||
                change.Property == StyledElement.DataContextProperty)
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

        private void ExpanderOnClick(object? sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent, this));
        }

        private void ExpanderOnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Handled)
            {
                return;
            }

            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent, this));
                e.Handled = true;
            }
        }

        private void UpdateIndentPadding()
        {
            var indent = Math.Max(Indent, 0);
            SetCurrentValue(PaddingProperty, new Thickness(Level * indent, 0, 0, 0));
        }

        private void AttachDirectSubscriptions()
        {
            DetachDirectSubscriptions();
            if (_directColumn == null || _node == null || VisualRoot == null)
            {
                return;
            }

            var nodeListener = _nodePropertyChangedListener ??=
                new WeakPropertyChangedListener<DataGridHierarchicalPresenter>(
                    this,
                    static (presenter, sender, e) => presenter.OnNodePropertyChanged(sender, e));
            _node.PropertyChanged += nodeListener.Handler;

            if (_directColumn.TrackDirectTextValueChanges &&
                _node.Item is INotifyPropertyChanged itemNotifier)
            {
                var itemListener = _itemPropertyChangedListener ??=
                    new WeakPropertyChangedListener<DataGridHierarchicalPresenter>(
                        this,
                        static (presenter, sender, e) => presenter.OnItemPropertyChanged(sender, e));
                _itemNotifier = itemNotifier;
                _itemNotifier.PropertyChanged += itemListener.Handler;
            }
        }

        private void DetachDirectSubscriptions()
        {
            if (_node != null && _nodePropertyChangedListener != null)
            {
                _node.PropertyChanged -= _nodePropertyChangedListener.Handler;
            }

            if (_itemNotifier != null && _itemPropertyChangedListener != null)
            {
                _itemNotifier.PropertyChanged -= _itemPropertyChangedListener.Handler;
                _itemNotifier = null;
            }
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!ReferenceEquals(sender, _node))
            {
                return;
            }

            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(HierarchicalNode.Level) ||
                e.PropertyName == nameof(HierarchicalNode.IsExpanded) ||
                e.PropertyName == nameof(HierarchicalNode.IsLeaf))
            {
                UpdateDirectState();
            }
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _itemNotifier))
            {
                UpdateDirectContent();
            }
        }

        private void UpdateDirectState()
        {
            if (_node == null)
            {
                SetCurrentValue(LevelProperty, 0);
                SetCurrentValue(IsExpandedProperty, false);
                SetCurrentValue(IsExpandableProperty, false);
                return;
            }

            SetCurrentValue(LevelProperty, _node.Level);
            SetCurrentValue(IsExpandedProperty, _node.IsExpanded);
            SetCurrentValue(IsExpandableProperty, !_node.IsLeaf);
        }

        private void UpdateDirectContent()
        {
            if (_directColumn == null)
            {
                return;
            }

            var value = _directColumn.GetDirectText(_node ?? DataContext);
            if (!Equals(Content, value))
            {
                SetCurrentValue(ContentProperty, value);
            }
        }
    }
}
