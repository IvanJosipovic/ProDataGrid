// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.DataGridHierarchical;
using Avalonia.Controls.Selection;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class SelectionModelItemSelectionViewModel : ObservableObject
    {
        public sealed class TreeItem
        {
            public TreeItem(int id, string name, TreeItem? parent = null)
            {
                Id = id;
                Name = name;
                Parent = parent;
                Children = new ObservableCollection<TreeItem>();
            }

            public int Id { get; }

            public string Name { get; }

            public TreeItem? Parent { get; }

            public ObservableCollection<TreeItem> Children { get; }
        }

        private readonly TreeItem? _deepItem;

        public SelectionModelItemSelectionViewModel()
        {
            Roots = BuildSample();
            _deepItem = FindDeepItem();

            var options = new HierarchicalOptions<TreeItem>
            {
                ChildrenSelector = item => item.Children,
                IsLeafSelector = item => item.Children.Count == 0,
                AutoExpandRoot = true,
                MaxAutoExpandDepth = 0,
                VirtualizeChildren = false
            };

            Model = new HierarchicalModel<TreeItem>(options);
            Model.SetRoots(Roots);
            Model.ExpandAll();

            SelectionModel = new SelectionModel<object> { SingleSelect = false };
            SelectionModel.SelectionChanged += (_, __) => OnPropertyChanged(nameof(SelectedCount));

            SelectRootCommand = new RelayCommand(_ => SelectItem(Roots.Count > 0 ? Roots[0] : null));
            SelectDeepItemCommand = new RelayCommand(_ => SelectItem(_deepItem));
            SelectSiblingPairCommand = new RelayCommand(_ => SelectSiblings());
            SelectRangeCommand = new RelayCommand(_ => SelectRange());
            SelectItemsCommand = new RelayCommand(_ => SelectItems());
            ClearSelectionCommand = new RelayCommand(_ => SelectionModel.Clear());
        }

        public ObservableCollection<TreeItem> Roots { get; }

        public HierarchicalModel<TreeItem> Model { get; }

        public SelectionModel<object> SelectionModel { get; }

        public RelayCommand SelectRootCommand { get; }

        public RelayCommand SelectDeepItemCommand { get; }

        public RelayCommand SelectSiblingPairCommand { get; }

        public RelayCommand SelectRangeCommand { get; }

        public RelayCommand SelectItemsCommand { get; }

        public RelayCommand ClearSelectionCommand { get; }

        public int SelectedCount => SelectionModel.SelectedItems.Count;

        private void SelectItem(TreeItem? item)
        {
            if (item == null || SelectionModel.Source == null)
            {
                return;
            }

            SelectionModel.Select(item);
        }

        private void SelectSiblings()
        {
            if (SelectionModel.Source == null || Roots.Count == 0)
            {
                return;
            }

            var siblings = Roots[0].Children;
            if (siblings.Count == 0)
            {
                return;
            }

            using (SelectionModel.BatchUpdate())
            {
                SelectionModel.Clear();
                var count = Math.Min(2, siblings.Count);
                for (var i = 0; i < count; i++)
                {
                    SelectionModel.Select(siblings[i]);
                }
            }
        }

        private void SelectRange()
        {
            if (SelectionModel.Source == null || Roots.Count == 0 || _deepItem == null)
            {
                return;
            }

            var start = Roots[0].Children.Count > 0 ? Roots[0].Children[0] : Roots[0];
            using (SelectionModel.BatchUpdate())
            {
                SelectionModel.Clear();
                SelectionModel.SelectRange(start, _deepItem);
            }
        }

        private void SelectItems()
        {
            if (SelectionModel.Source == null || Roots.Count == 0)
            {
                return;
            }

            var items = new List<TreeItem>
            {
                Roots[0]
            };

            if (Roots[0].Children.Count > 0)
            {
                items.Add(Roots[0].Children[0]);
            }

            if (_deepItem != null)
            {
                items.Add(_deepItem);
            }

            if (Roots.Count > 1)
            {
                items.Add(Roots[Roots.Count - 1]);
            }

            if (items.Count == 0)
            {
                return;
            }

            using (SelectionModel.BatchUpdate())
            {
                SelectionModel.Clear();
                SelectionModel.SelectRange(items);
            }
        }

        private TreeItem? FindDeepItem()
        {
            foreach (var root in Roots)
            {
                foreach (var child in root.Children)
                {
                    if (child.Children.Count > 0)
                    {
                        return child.Children[0];
                    }
                }
            }

            return null;
        }

        private static ObservableCollection<TreeItem> BuildSample()
        {
            var roots = new ObservableCollection<TreeItem>();
            var id = 1;

            for (var group = 1; group <= 3; group++)
            {
                var root = new TreeItem(id++, $"Group {group}");
                for (var child = 1; child <= 3; child++)
                {
                    var childNode = new TreeItem(id++, $"Item {group}.{child}", root);
                    if (child == 2)
                    {
                        for (var leaf = 1; leaf <= 2; leaf++)
                        {
                            childNode.Children.Add(new TreeItem(id++, $"Item {group}.{child}.{leaf}", childNode));
                        }
                    }
                    root.Children.Add(childNode);
                }
                roots.Add(root);
            }

            return roots;
        }
    }
}
