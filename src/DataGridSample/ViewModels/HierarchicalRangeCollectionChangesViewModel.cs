using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Avalonia.Controls.DataGridHierarchical;
using DataGridSample.Collections;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class HierarchicalRangeCollectionChangesViewModel : ObservableObject
    {
        private readonly Random _random = new Random();
        private int _seed;
        private int _batchSize = 4;
        private int _childrenPerRoot = 3;
        private int _rootInsertIndex;
        private int _rootRemoveIndex;
        private int _childInsertIndex;
        private int _childRemoveIndex;
        private object? _selectedNode;
        private HierarchicalStreamingItem? _selectedItem;
        private ObservableRangeCollection<HierarchicalStreamingItem>? _selectedChildren;
        private int _rootCount;
        private int _visibleCount;

        public HierarchicalRangeCollectionChangesViewModel()
        {
            RootItems = new ObservableRangeCollection<HierarchicalStreamingItem>();
            Events = new ObservableCollection<string>();

            var options = new HierarchicalOptions<HierarchicalStreamingItem>
            {
                ChildrenSelector = item => item.Children,
                IsLeafSelector = item => item.Children.Count == 0,
                IsExpandedSelector = item => item.IsExpanded,
                IsExpandedSetter = (item, value) => item.IsExpanded = value
            };

            Model = new HierarchicalModel<HierarchicalStreamingItem>(options);
            Model.SetRoots(RootItems);

            RootItems.CollectionChanged += (_, e) =>
            {
                LogChange("Root", e);
                UpdateCounts();
            };
            Model.FlattenedChanged += (_, __) => UpdateCounts();

            AddRootRangeCommand = new RelayCommand(_ => AddRootRange());
            InsertRootRangeCommand = new RelayCommand(_ => InsertRootRange());
            RemoveRootRangeCommand = new RelayCommand(_ => RemoveRootRange());
            AddChildRangeCommand = new RelayCommand(_ => AddChildRange(), _ => SelectedItem != null);
            InsertChildRangeCommand = new RelayCommand(_ => InsertChildRange(), _ => SelectedItem != null);
            RemoveChildRangeCommand = new RelayCommand(_ => RemoveChildRange(), _ => SelectedItem != null);
            ResetCommand = new RelayCommand(_ => ResetItems());

            ResetItems();
        }

        public ObservableRangeCollection<HierarchicalStreamingItem> RootItems { get; }

        public HierarchicalModel<HierarchicalStreamingItem> Model { get; }

        public ObservableCollection<string> Events { get; }

        public RelayCommand AddRootRangeCommand { get; }
        public RelayCommand InsertRootRangeCommand { get; }
        public RelayCommand RemoveRootRangeCommand { get; }
        public RelayCommand AddChildRangeCommand { get; }
        public RelayCommand InsertChildRangeCommand { get; }
        public RelayCommand RemoveChildRangeCommand { get; }
        public RelayCommand ResetCommand { get; }

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, Math.Max(1, value));
        }

        public int ChildrenPerRoot
        {
            get => _childrenPerRoot;
            set => SetProperty(ref _childrenPerRoot, Math.Max(0, value));
        }

        public int RootInsertIndex
        {
            get => _rootInsertIndex;
            set => SetProperty(ref _rootInsertIndex, Math.Max(0, value));
        }

        public int RootRemoveIndex
        {
            get => _rootRemoveIndex;
            set => SetProperty(ref _rootRemoveIndex, Math.Max(0, value));
        }

        public int ChildInsertIndex
        {
            get => _childInsertIndex;
            set => SetProperty(ref _childInsertIndex, Math.Max(0, value));
        }

        public int ChildRemoveIndex
        {
            get => _childRemoveIndex;
            set => SetProperty(ref _childRemoveIndex, Math.Max(0, value));
        }

        public object? SelectedNode
        {
            get => _selectedNode;
            set
            {
                if (SetProperty(ref _selectedNode, value))
                {
                    UpdateSelectedItem();
                }
            }
        }

        public HierarchicalStreamingItem? SelectedItem => _selectedItem;

        public string SelectedLabel =>
            _selectedItem == null
                ? "Selected: none"
                : $"Selected: {_selectedItem.Name} (children: {_selectedItem.Children.Count})";

        public int RootCount
        {
            get => _rootCount;
            private set => SetProperty(ref _rootCount, value);
        }

        public int VisibleCount
        {
            get => _visibleCount;
            private set => SetProperty(ref _visibleCount, value);
        }

        private void AddRootRange()
        {
            RootItems.AddRange(CreateRoots(BatchSize));
        }

        private void InsertRootRange()
        {
            var index = Math.Clamp(RootInsertIndex, 0, RootItems.Count);
            RootItems.InsertRange(index, CreateRoots(BatchSize));
        }

        private void RemoveRootRange()
        {
            if (RootItems.Count == 0)
            {
                return;
            }

            var index = Math.Clamp(RootRemoveIndex, 0, RootItems.Count - 1);
            var count = Math.Min(BatchSize, RootItems.Count - index);
            RootItems.RemoveRange(index, count);
        }

        private void AddChildRange()
        {
            var item = SelectedItem;
            if (item == null)
            {
                return;
            }

            item.Children.AddRange(CreateChildren(item, BatchSize));
            EnsureExpanded(item);
        }

        private void InsertChildRange()
        {
            var item = SelectedItem;
            if (item == null)
            {
                return;
            }

            var index = Math.Clamp(ChildInsertIndex, 0, item.Children.Count);
            item.Children.InsertRange(index, CreateChildren(item, BatchSize));
            EnsureExpanded(item);
        }

        private void RemoveChildRange()
        {
            var item = SelectedItem;
            if (item == null || item.Children.Count == 0)
            {
                return;
            }

            var index = Math.Clamp(ChildRemoveIndex, 0, item.Children.Count - 1);
            var count = Math.Min(BatchSize, item.Children.Count - index);
            item.Children.RemoveRange(index, count);

            if (item.Children.Count == 0 && item.IsExpanded)
            {
                item.IsExpanded = false;
                Model.Collapse(new[] { item });
            }
        }

        private void ResetItems()
        {
            _seed = 0;
            RootItems.ResetWith(CreateRoots(10));
            SelectedNode = RootItems.Count > 0 ? RootItems[0] : null;
            RootInsertIndex = Math.Min(RootInsertIndex, RootItems.Count);
            RootRemoveIndex = Math.Min(RootRemoveIndex, Math.Max(0, RootItems.Count - 1));
            UpdateCounts();
        }

        private IEnumerable<HierarchicalStreamingItem> CreateRoots(int count)
        {
            var items = new List<HierarchicalStreamingItem>(count);
            for (var i = 0; i < count; i++)
            {
                items.Add(CreateRoot());
            }

            return items;
        }

        private HierarchicalStreamingItem CreateRoot()
        {
            var id = NextId();
            var root = CreateItem(id, $"Root {id}", isExpanded: true);

            if (ChildrenPerRoot > 0)
            {
                var children = new List<HierarchicalStreamingItem>(ChildrenPerRoot);
                for (var i = 0; i < ChildrenPerRoot; i++)
                {
                    children.Add(CreateChild(root.Id, i + 1));
                }

                root.Children.AddRange(children);
            }

            return root;
        }

        private HierarchicalStreamingItem CreateChild(int parentId, int index)
        {
            var id = NextId();
            return CreateItem(id, $"Item {parentId}-{index}", isExpanded: false);
        }

        private IEnumerable<HierarchicalStreamingItem> CreateChildren(HierarchicalStreamingItem parent, int count)
        {
            var items = new List<HierarchicalStreamingItem>(count);
            for (var i = 0; i < count; i++)
            {
                var id = NextId();
                items.Add(CreateItem(id, $"{parent.Name}.{id}", isExpanded: false));
            }

            return items;
        }

        private HierarchicalStreamingItem CreateItem(int id, string name, bool isExpanded)
        {
            var price = Math.Round(_random.NextDouble() * 1000, 2);
            var updatedAt = DateTime.Now;
            return new HierarchicalStreamingItem(id, name, price, updatedAt, isExpanded);
        }

        private int NextId()
        {
            return ++_seed;
        }

        private void EnsureExpanded(HierarchicalStreamingItem item)
        {
            if (!item.IsExpanded)
            {
                item.IsExpanded = true;
                Model.Expand(new[] { item });
            }
        }

        private void UpdateSelectedItem()
        {
            var item = ResolveSelectedItem(_selectedNode);
            if (ReferenceEquals(item, _selectedItem))
            {
                return;
            }

            if (_selectedChildren != null)
            {
                _selectedChildren.CollectionChanged -= OnSelectedChildrenChanged;
            }

            _selectedItem = item;
            _selectedChildren = item?.Children;

            if (_selectedChildren != null)
            {
                _selectedChildren.CollectionChanged += OnSelectedChildrenChanged;
            }

            OnPropertyChanged(nameof(SelectedItem));
            OnPropertyChanged(nameof(SelectedLabel));
            RaiseChildCommandState();
        }

        private void OnSelectedChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_selectedItem != null)
            {
                LogChange($"Child({_selectedItem.Name})", e);
                OnPropertyChanged(nameof(SelectedLabel));
            }
        }

        private void RaiseChildCommandState()
        {
            AddChildRangeCommand.RaiseCanExecuteChanged();
            InsertChildRangeCommand.RaiseCanExecuteChanged();
            RemoveChildRangeCommand.RaiseCanExecuteChanged();
        }

        private HierarchicalStreamingItem? ResolveSelectedItem(object? node)
        {
            if (node is HierarchicalNode hierarchicalNode)
            {
                return hierarchicalNode.Item as HierarchicalStreamingItem;
            }

            return node as HierarchicalStreamingItem;
        }

        private void LogChange(string scope, NotifyCollectionChangedEventArgs e)
        {
            var message = e.Action switch
            {
                NotifyCollectionChangedAction.Add => $"{scope} Add: {DescribeCount(e.NewItems)} at {e.NewStartingIndex}",
                NotifyCollectionChangedAction.Remove => $"{scope} Remove: {DescribeCount(e.OldItems)} from {e.OldStartingIndex}",
                NotifyCollectionChangedAction.Move => $"{scope} Move: {DescribeCount(e.OldItems)} to {e.NewStartingIndex}",
                NotifyCollectionChangedAction.Replace => $"{scope} Replace: {DescribeCount(e.OldItems)} -> {DescribeCount(e.NewItems)}",
                NotifyCollectionChangedAction.Reset => $"{scope} Reset",
                _ => $"{scope} {e.Action}"
            };

            Events.Insert(0, message);
            if (Events.Count > 50)
            {
                Events.RemoveAt(Events.Count - 1);
            }
        }

        private void UpdateCounts()
        {
            RootCount = RootItems.Count;
            VisibleCount = Model.Count;
        }

        private static string DescribeCount(System.Collections.IList? items)
        {
            return items == null ? "0" : items.Count.ToString();
        }
    }
}
