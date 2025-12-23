// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.DataGridHierarchical;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class HierarchicalPathSelectionViewModel : ObservableObject
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

        private TreeItem? _selectedItem;
        private string _selectedLabel = "None";
        private string _selectedPath = "N/A";
        private string _pathInput = "0/1/0";
        private string _status = "Use a path like 0/1/0 to select a node.";

        public HierarchicalPathSelectionViewModel()
        {
            Roots = BuildSample();

            var options = new HierarchicalOptions<TreeItem>
            {
                ChildrenSelector = item => item.Children,
                IsLeafSelector = item => item.Children.Count == 0,
                AutoExpandRoot = true,
                MaxAutoExpandDepth = 1,
                VirtualizeChildren = false,
                ItemPathSelector = BuildPath,
                ExpandedStateKeyMode = ExpandedStateKeyMode.Path
            };

            Model = new HierarchicalModel<TreeItem>(options);
            Model.SetRoots(Roots);

            SelectPathCommand = new RelayCommand(param => SelectByPath(param as string));
            ApplyPathCommand = new RelayCommand(_ => SelectByPath(PathInput));
            ExpandAllCommand = new RelayCommand(_ => Model.ExpandAll());
            CollapseAllCommand = new RelayCommand(_ => Model.CollapseAll());
            RebuildCommand = new RelayCommand(_ => Model.SetRoots(Roots));

            SelectByPath(PathInput);
        }

        public ObservableCollection<TreeItem> Roots { get; }

        public HierarchicalModel<TreeItem> Model { get; }

        public RelayCommand SelectPathCommand { get; }

        public RelayCommand ApplyPathCommand { get; }

        public RelayCommand ExpandAllCommand { get; }

        public RelayCommand CollapseAllCommand { get; }

        public RelayCommand RebuildCommand { get; }

        public TreeItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    SelectedLabel = value == null ? "None" : $"{value.Name} (#{value.Id})";
                    SelectedPath = FormatPath(value == null ? null : BuildPath(value));
                    Status = value == null ? "Selection cleared." : $"Selected {SelectedLabel}.";
                }
            }
        }

        public string SelectedLabel
        {
            get => _selectedLabel;
            private set => SetProperty(ref _selectedLabel, value);
        }

        public string SelectedPath
        {
            get => _selectedPath;
            private set => SetProperty(ref _selectedPath, value);
        }

        public string PathInput
        {
            get => _pathInput;
            set => SetProperty(ref _pathInput, value);
        }

        public string Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        private void SelectByPath(string? pathText)
        {
            var path = ParsePath(pathText);
            if (path == null || path.Count == 0)
            {
                Status = "Enter a path like 0/1/0.";
                return;
            }

            var item = TryGetItemByPath(path);
            if (item == null)
            {
                Status = $"No item at path {FormatPath(path)}.";
                return;
            }

            SelectedItem = item;
        }

        private TreeItem? TryGetItemByPath(IReadOnlyList<int> path)
        {
            if (path.Count == 0)
            {
                return null;
            }

            var rootIndex = path[0];
            if (rootIndex < 0 || rootIndex >= Roots.Count)
            {
                return null;
            }

            var current = Roots[rootIndex];
            for (int i = 1; i < path.Count; i++)
            {
                var childIndex = path[i];
                if (childIndex < 0 || childIndex >= current.Children.Count)
                {
                    return null;
                }

                current = current.Children[childIndex];
            }

            return current;
        }

        private IReadOnlyList<int>? BuildPath(TreeItem item)
        {
            var segments = new List<int>();
            var current = item;

            while (current != null)
            {
                if (current.Parent == null)
                {
                    var rootIndex = IndexOfReference(Roots, current);
                    if (rootIndex < 0)
                    {
                        return null;
                    }

                    segments.Add(rootIndex);
                    break;
                }

                var parent = current.Parent;
                var childIndex = IndexOfReference(parent.Children, current);
                if (childIndex < 0)
                {
                    return null;
                }

                segments.Add(childIndex);
                current = parent;
            }

            segments.Reverse();
            return segments;
        }

        private static int IndexOfReference(IList<TreeItem> items, TreeItem target)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (ReferenceEquals(items[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        private static IReadOnlyList<int>? ParsePath(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var parts = text.Split(new[] { '/', '.', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<int>(parts.Length);

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var index) || index < 0)
                {
                    return null;
                }

                list.Add(index);
            }

            return list;
        }

        private static string FormatPath(IReadOnlyList<int>? path)
        {
            if (path == null || path.Count == 0)
            {
                return "N/A";
            }

            return string.Join("/", path);
        }

        private static ObservableCollection<TreeItem> BuildSample()
        {
            var roots = new ObservableCollection<TreeItem>();
            var id = 1;

            var rootA = new TreeItem(id++, "Group A");
            var rootB = new TreeItem(id++, "Group B");

            roots.Add(rootA);
            roots.Add(rootB);

            var a1 = new TreeItem(id++, "Node", rootA);
            var a2 = new TreeItem(id++, "Node", rootA);
            rootA.Children.Add(a1);
            rootA.Children.Add(a2);

            a1.Children.Add(new TreeItem(id++, "Leaf", a1));
            a1.Children.Add(new TreeItem(id++, "Leaf", a1));
            a2.Children.Add(new TreeItem(id++, "Leaf", a2));

            var b1 = new TreeItem(id++, "Node", rootB);
            rootB.Children.Add(b1);
            b1.Children.Add(new TreeItem(id++, "Leaf", b1));
            b1.Children.Add(new TreeItem(id++, "Leaf", b1));

            return roots;
        }
    }
}
