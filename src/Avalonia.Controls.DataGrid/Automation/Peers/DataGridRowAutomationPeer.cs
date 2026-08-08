using System;
using System.ComponentModel;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.DataGridHierarchical;

namespace Avalonia.Automation.Peers
{
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    class DataGridRowAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
    {
        private readonly DataGridRow _row;
        private HierarchicalNode? _node;
        private ExpandCollapseState _lastExpandCollapseState;

        public DataGridRowAutomationPeer(DataGridRow owner)
            : base(owner)
        {
            _row = owner;
            _row.PropertyChanged += OnRowPropertyChanged;
            AttachNode(owner.DataContext as HierarchicalNode);
        }

        /// <inheritdoc />
        public ExpandCollapseState ExpandCollapseState => GetExpandCollapseState(_node);

        /// <inheritdoc />
        public bool ShowsMenu => false;

        /// <inheritdoc />
        public void Expand()
        {
            if (_node is { IsLeaf: false } node && _row.OwningGrid is { } grid)
            {
                grid.HierarchicalModel.Expand(node);
            }
        }

        /// <inheritdoc />
        public void Collapse()
        {
            if (_node is { IsLeaf: false } node && _row.OwningGrid is { } grid)
            {
                grid.HierarchicalModel.Collapse(node);
            }
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.DataItem;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;

        private static ExpandCollapseState GetExpandCollapseState(HierarchicalNode? node)
        {
            if (node == null || node.IsLeaf)
            {
                return ExpandCollapseState.LeafNode;
            }

            if (node.IsLoading)
            {
                return ExpandCollapseState.PartiallyExpanded;
            }

            return node.IsExpanded
                ? ExpandCollapseState.Expanded
                : ExpandCollapseState.Collapsed;
        }

        private void OnRowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.DataContextProperty)
            {
                AttachNode(e.NewValue as HierarchicalNode);
            }
        }

        private void AttachNode(HierarchicalNode? node)
        {
            ExpandCollapseState oldState = _lastExpandCollapseState;
            if (_node != null)
            {
                _node.PropertyChanged -= OnNodePropertyChanged;
            }

            _node = node;
            if (_node != null)
            {
                _node.PropertyChanged += OnNodePropertyChanged;
            }

            _lastExpandCollapseState = GetExpandCollapseState(_node);
            RaiseExpandCollapseChanges(oldState, _lastExpandCollapseState);
        }

        private void OnNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName) &&
                e.PropertyName != nameof(HierarchicalNode.IsExpanded) &&
                e.PropertyName != nameof(HierarchicalNode.IsLeaf) &&
                e.PropertyName != nameof(HierarchicalNode.IsLoading))
            {
                return;
            }

            ExpandCollapseState newState = GetExpandCollapseState(_node);
            ExpandCollapseState oldState = _lastExpandCollapseState;
            _lastExpandCollapseState = newState;
            RaiseExpandCollapseChanges(oldState, newState);
        }

        private void RaiseExpandCollapseChanges(ExpandCollapseState oldState, ExpandCollapseState newState)
        {
            if (oldState == newState)
            {
                return;
            }

            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                oldState,
                newState);
            RaiseChildrenChangedEvent();
        }
    }
}
