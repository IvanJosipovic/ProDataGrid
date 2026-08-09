// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.ComponentModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Utils;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

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
        private const string ExpanderSizeResourceKey = "DataGridHierarchicalExpanderSize";
        private const string ExpanderGlyphSizeResourceKey = "DataGridHierarchicalExpanderGlyphSize";
        private const string ContentMarginResourceKey = "DataGridHierarchicalPresenterContentMargin";
        private const string DisabledGlyphOpacityResourceKey = "DataGridHierarchicalDisabledGlyphOpacity";
        private const string CollapsedGlyphResourceKey = "DataGridRowGroupHeaderIconClosedPath";
        private const string ExpandedGlyphResourceKey = "DataGridRowGroupHeaderIconOpenedPath";
        private const double DefaultExpanderSize = 28d;
        private const double DefaultExpanderGlyphSize = 10d;
        private const double MaximumTapMovement = 8d;
        private HierarchicalNode? _node;
        private ToggleButton? _expander;
        private Control? _contentRoot;
        private TextBlock? _textElement;
        private DataGridHierarchicalColumn? _column;
        private DataGridHierarchicalColumn? _textColumn;
        private bool _textAccessorConfigurationInitialized;
        private INotifyPropertyChanged? _itemNotifier;
        private bool _nodeSubscribed;
        private double _expanderSize = DefaultExpanderSize;
        private double _expanderGlyphSize = DefaultExpanderGlyphSize;
        private Thickness _contentMargin = new(6, 0, 0, 0);
        private double _disabledGlyphOpacity;
        private Geometry? _collapsedGlyph;
        private Geometry? _expandedGlyph;
        private bool _leanExpanderKeyboardActive;
        private IPointer? _leanExpanderPressedPointer;
        private Point _leanExpanderPressedPoint;
        private readonly WeakPropertyChangedListener<DataGridDirectHierarchicalCell> _nodePropertyChangedListener;
        private readonly WeakPropertyChangedListener<DataGridDirectHierarchicalCell> _itemPropertyChangedListener;

        static DataGridDirectHierarchicalCell()
        {
            UseDirectChromeProperty.OverrideDefaultValue<DataGridDirectHierarchicalCell>(true);
            AffectsRender<DataGridDirectHierarchicalCell>(
                BackgroundProperty,
                BorderBrushProperty,
                BorderThicknessProperty,
                CornerRadiusProperty,
                ForegroundProperty,
                LevelProperty,
                IndentProperty,
                IsExpandedProperty,
                IsExpandableProperty);
            PointerPressedEvent.AddClassHandler<DataGridDirectHierarchicalCell>(
                static (cell, e) => cell.OnLeanExpanderPointerPressed(e),
                RoutingStrategies.Tunnel,
                handledEventsToo: true);
            PointerReleasedEvent.AddClassHandler<DataGridDirectHierarchicalCell>(
                static (cell, e) => cell.OnLeanExpanderPointerReleased(e),
                RoutingStrategies.Bubble,
                handledEventsToo: true);
        }

        public DataGridDirectHierarchicalCell()
        {
            _nodePropertyChangedListener = new WeakPropertyChangedListener<DataGridDirectHierarchicalCell>(
                this,
                static (cell, sender, e) => cell.NodeOnPropertyChanged(sender, e));
            _itemPropertyChangedListener = new WeakPropertyChangedListener<DataGridDirectHierarchicalCell>(
                this,
                static (cell, sender, e) => cell.ItemOnPropertyChanged(sender, e));
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

        internal bool ConfigureTextAccessor(DataGridHierarchicalColumn? column, object? dataItem)
        {
            _column = column;
            _textColumn = column?.CanUseDirectTextContentFor(dataItem) == true ? column : null;
            _textAccessorConfigurationInitialized = true;
            UpdateTextSubscription();
            return _textColumn != null;
        }

        internal bool ConfigureTextAccessor(DataGridHierarchicalColumn? column) =>
            ConfigureTextAccessor(column, DataContext);

        internal bool UsesTextAccessor => _textColumn != null;

        internal bool TextAccessorConfigurationInitialized => _textAccessorConfigurationInitialized;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            SubscribeNode();
            UpdateTextSubscription();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _leanExpanderPressedPointer = null;
            _leanExpanderKeyboardActive = false;
            DetachTextSubscription();
            UnsubscribeNode();
            base.OnDetachedFromVisualTree(e);
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
                if (_column != null)
                {
                    _column.ConfigureDirectHierarchicalCell(
                        this,
                        change.NewValue,
                        preserveCompatibleMode: true);
                }
                else
                {
                    UpdateTextSubscription();
                }
            }
            else if (change.Property == LevelProperty || change.Property == IndentProperty)
            {
                UpdateIndentPadding();
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
            RenderLeanExpander(context);
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
            _contentRoot ??= _textElement;
            if (_expander != null)
            {
                _expander.Click += ExpanderOnClick;
                _expander.KeyDown += ExpanderOnKeyDown;
            }
            else
            {
                ResolveLeanExpanderResources();
            }

            UpdateExpanderState();
            UpdateIndentPadding();
            UpdateTextElement();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (_expander == null &&
                _leanExpanderKeyboardActive &&
                IsExpandable &&
                !e.Handled &&
                (e.Key == Key.Enter || e.Key == Key.Space))
            {
                RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent, this));
                e.Handled = true;
            }
        }

        protected override void OnLostFocus(FocusChangedEventArgs e)
        {
            _leanExpanderKeyboardActive = false;
            base.OnLostFocus(e);
        }

        private void AttachNode(HierarchicalNode? node)
        {
            if (ReferenceEquals(_node, node))
            {
                SubscribeNode();
                UpdateNodeState();
                return;
            }

            UnsubscribeNode();

            _node = node;
            SubscribeNode();

            UpdateNodeState();
        }

        private void SubscribeNode()
        {
            if (_node == null || _nodeSubscribed || VisualRoot == null)
            {
                return;
            }

            _node.PropertyChanged += _nodePropertyChangedListener.Handler;
            _nodeSubscribed = true;
        }

        private void UnsubscribeNode()
        {
            if (_node == null || !_nodeSubscribed)
            {
                return;
            }

            _node.PropertyChanged -= _nodePropertyChangedListener.Handler;
            _nodeSubscribed = false;
        }

        private void UpdateTextSubscription()
        {
            DetachTextSubscription();

            if (_textColumn == null)
            {
                Value = null;
                return;
            }

            UpdateTextValue();
            if (_textColumn.TrackDirectTextValueChanges &&
                VisualRoot != null &&
                _node?.Item is INotifyPropertyChanged notifier)
            {
                _itemNotifier = notifier;
                _itemNotifier.PropertyChanged += _itemPropertyChangedListener.Handler;
            }
        }

        private void DetachTextSubscription()
        {
            if (_itemNotifier == null)
            {
                return;
            }

            _itemNotifier.PropertyChanged -= _itemPropertyChangedListener.Handler;
            _itemNotifier = null;
        }

        private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _itemNotifier))
            {
                UpdateTextValue();
            }
        }

        private void UpdateTextValue()
        {
            Value = _textColumn?.GetDirectText(DataContext);
        }

        private void NodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _node) &&
                (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName == nameof(HierarchicalNode.Level) ||
                e.PropertyName == nameof(HierarchicalNode.IsExpanded) ||
                e.PropertyName == nameof(HierarchicalNode.IsLeaf)))
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
                _contentRoot.Margin = _expander == null
                    ? new Thickness(
                        padding.Left + _expanderSize + _contentMargin.Left,
                        _contentMargin.Top,
                        _contentMargin.Right,
                        _contentMargin.Bottom)
                    : padding;
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

        internal bool IsLeanExpanderHit(Point point)
        {
            if (_expander != null)
            {
                return false;
            }

            var indent = Math.Max(Level, 0) * Math.Max(Indent, 0);
            return point.X >= indent &&
                   point.X <= indent + _expanderSize &&
                   point.Y >= 0 &&
                   point.Y <= Bounds.Height;
        }

        private void OnLeanExpanderPointerPressed(PointerPressedEventArgs e)
        {
            _leanExpanderPressedPointer = null;
            _leanExpanderKeyboardActive = false;
            if (_expander != null)
            {
                return;
            }

            var point = e.GetCurrentPoint(this);
            var isTouchLike = e.Pointer.Type is PointerType.Touch or PointerType.Pen;
            var isPrimaryPressed = point.Properties.IsLeftButtonPressed || isTouchLike;
            var isExpanderHit = isPrimaryPressed && IsLeanExpanderHit(point.Position);
            if (isExpanderHit)
            {
                _leanExpanderPressedPointer = e.Pointer;
                _leanExpanderPressedPoint = point.Position;
            }
        }

        private void OnLeanExpanderPointerReleased(PointerReleasedEventArgs e)
        {
            if (_expander != null || !ReferenceEquals(e.Pointer, _leanExpanderPressedPointer))
            {
                return;
            }

            _leanExpanderPressedPointer = null;
            var point = e.GetPosition(this);
            var delta = point - _leanExpanderPressedPoint;
            var isTap = Math.Abs(delta.X) <= MaximumTapMovement &&
                        Math.Abs(delta.Y) <= MaximumTapMovement;
            if (!isTap || !IsLeanExpanderHit(point))
            {
                return;
            }

            _leanExpanderKeyboardActive = true;
            if (IsExpandable)
            {
                RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent, this));
                e.Handled = true;
            }
        }

        private void ResolveLeanExpanderResources()
        {
            _expanderSize = FindResource(ExpanderSizeResourceKey, DefaultExpanderSize);
            _expanderGlyphSize = FindResource(ExpanderGlyphSizeResourceKey, DefaultExpanderGlyphSize);
            _contentMargin = FindResource(ContentMarginResourceKey, new Thickness(6, 0, 0, 0));
            _disabledGlyphOpacity = FindResource(DisabledGlyphOpacityResourceKey, 0d);
            _collapsedGlyph = FindResource<Geometry?>(CollapsedGlyphResourceKey, null);
            _expandedGlyph = FindResource<Geometry?>(ExpandedGlyphResourceKey, null);
        }

        private T FindResource<T>(string key, T fallback)
        {
            return this.TryFindResource(key, out var resource) && resource is T value
                ? value
                : fallback;
        }

        private void RenderLeanExpander(DrawingContext context)
        {
            if (_expander != null || Foreground == null || _expanderGlyphSize <= 0 || _expanderSize <= 0)
            {
                return;
            }

            var opacity = IsExpandable ? 1d : _disabledGlyphOpacity;
            if (opacity <= 0)
            {
                return;
            }

            var geometry = IsExpanded ? _expandedGlyph : _collapsedGlyph;
            if (geometry == null)
            {
                return;
            }

            var geometryBounds = geometry.Bounds;
            if (geometryBounds.Width <= 0 || geometryBounds.Height <= 0)
            {
                return;
            }

            var scale = Math.Min(
                _expanderGlyphSize / geometryBounds.Width,
                _expanderGlyphSize / geometryBounds.Height);
            var indent = Math.Max(Level, 0) * Math.Max(Indent, 0);
            var renderedWidth = geometryBounds.Width * scale;
            var renderedHeight = geometryBounds.Height * scale;
            var offsetX = indent + ((_expanderSize - renderedWidth) / 2d) - (geometryBounds.X * scale);
            var offsetY = ((Bounds.Height - renderedHeight) / 2d) - (geometryBounds.Y * scale);
            using var opacityState = context.PushOpacity(opacity);
            using var transformState = context.PushTransform(new Matrix(scale, 0, 0, scale, offsetX, offsetY));
            context.DrawGeometry(Foreground, null, geometry);
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
