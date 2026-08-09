// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.ComponentModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

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
        private const string PartContentRoot = "PART_ContentRoot";
        private const string PartText = "PART_Text";
        private HierarchicalNode? _node;
        private ToggleButton? _expander;
        private Control? _contentRoot;
        private TextBlock? _textElement;
        private DataGridHierarchicalColumn? _textColumn;
        private INotifyPropertyChanged? _itemNotifier;
        private IBrush? _cachedBorderBrush;
        private double _cachedBorderThickness;
        private Pen? _cachedBorderPen;

        static DataGridDirectHierarchicalCell()
        {
            AffectsRender<DataGridDirectHierarchicalCell>(
                BackgroundProperty,
                BorderBrushProperty,
                BorderThicknessProperty,
                CornerRadiusProperty);
        }

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

        /// <summary>Defines the <see cref="Value"/> property.</summary>
        public static readonly DirectProperty<DataGridDirectHierarchicalCell, string?> ValueProperty =
            AvaloniaProperty.RegisterDirect<DataGridDirectHierarchicalCell, string?>(
                nameof(Value),
                cell => cell.Value,
                (cell, value) => cell.Value = value);

        private string? _value;

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

        /// <summary>Gets or sets the text shown by the optimized retained text theme.</summary>
        public string? Value
        {
            get => _value;
            set => SetAndRaise(ValueProperty, ref _value, value);
        }

        internal bool ConfigureTextAccessor(DataGridHierarchicalColumn? column)
        {
            _textColumn = column?.CanUseDirectTextContent == true ? column : null;
            UpdateTextSubscription();
            return _textColumn != null;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == DataContextProperty)
            {
                AttachNode(change.NewValue switch
                {
                    HierarchicalNode node => node,
                    IHierarchicalNodeItem nodeItem => nodeItem.Node,
                    _ => null
                });
                UpdateTextSubscription();
            }
            else if (change.Property == LevelProperty || change.Property == IndentProperty)
            {
                UpdateIndentPadding();
            }
            else if (change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty)
            {
                _cachedBorderBrush = null;
                _cachedBorderPen = null;
                _cachedBorderThickness = 0d;
            }
            else if (change.Property == IsExpandedProperty || change.Property == IsExpandableProperty)
            {
                UpdateExpanderState();
            }
            else if (change.Property == ValueProperty)
            {
                UpdateTextElement();
            }
        }

        /// <inheritdoc />
        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var thickness = Math.Max(
                Math.Max(BorderThickness.Left, BorderThickness.Top),
                Math.Max(BorderThickness.Right, BorderThickness.Bottom));
            Pen? borderPen = null;
            if (BorderBrush != null && thickness > 0d)
            {
                if (!ReferenceEquals(_cachedBorderBrush, BorderBrush) ||
                    !_cachedBorderThickness.Equals(thickness))
                {
                    _cachedBorderBrush = BorderBrush;
                    _cachedBorderThickness = thickness;
                    _cachedBorderPen = new Pen(BorderBrush, thickness);
                }

                borderPen = _cachedBorderPen;
            }

            if (Background == null && borderPen == null)
            {
                return;
            }

            var inset = borderPen == null ? 0d : thickness * 0.5d;
            var chromeBounds = new Rect(
                inset,
                inset,
                Math.Max(0d, Bounds.Width - (inset * 2d)),
                Math.Max(0d, Bounds.Height - (inset * 2d)));
            context.DrawRectangle(
                Background,
                borderPen,
                new RoundedRect(chromeBounds, CornerRadius));
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
            _contentRoot = e.NameScope.Find<Control>(PartContentRoot);
            _textElement = e.NameScope.Find<TextBlock>(PartText);
            if (_expander != null)
            {
                _expander.Click += ExpanderOnClick;
                _expander.KeyDown += ExpanderOnKeyDown;
            }

            UpdateExpanderState();
            UpdateIndentPadding();
            UpdateTextElement();
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

        private void UpdateTextSubscription()
        {
            if (_itemNotifier != null)
            {
                _itemNotifier.PropertyChanged -= ItemOnPropertyChanged;
                _itemNotifier = null;
            }

            if (_textColumn == null)
            {
                Value = null;
                return;
            }

            UpdateTextValue();
            if (_textColumn.TrackDirectTextValueChanges &&
                _node?.Item is INotifyPropertyChanged notifier)
            {
                _itemNotifier = notifier;
                _itemNotifier.PropertyChanged += ItemOnPropertyChanged;
            }
        }

        private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            UpdateTextValue();
        }

        private void UpdateTextValue()
        {
            Value = _textColumn?.GetDirectText(DataContext);
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
            var padding = new Thickness(Level * Math.Max(Indent, 0), 0, 0, 0);
            SetCurrentValue(PaddingProperty, padding);
            if (_contentRoot != null)
            {
                _contentRoot.Margin = padding;
            }
        }

        private void UpdateExpanderState()
        {
            if (_expander != null)
            {
                _expander.IsEnabled = IsExpandable;
                _expander.IsChecked = IsExpanded;
            }
        }

        private void UpdateTextElement()
        {
            if (_textElement != null && !string.Equals(_textElement.Text, Value, StringComparison.Ordinal))
            {
                _textElement.Text = Value;
            }
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
