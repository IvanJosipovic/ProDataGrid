using System;
using System.Collections.ObjectModel;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class NestedDataGridViewModel : ObservableObject
    {
        public NestedDataGridViewModel()
        {
            Groups = new ObservableCollection<NestedDataGridGroup>
            {
                CreateGroup("Group A", 3),
                CreateGroup("Group B", 1),
                CreateGroup("Group C", 2),
            };
        }

        public ObservableCollection<NestedDataGridGroup> Groups { get; }

        private static NestedDataGridGroup CreateGroup(string name, int count)
        {
            var group = new NestedDataGridGroup(name);
            for (var i = 0; i < count; i++)
            {
                group.AddRow();
            }

            return group;
        }
    }

    public class NestedDataGridGroup : ObservableObject
    {
        private readonly Random _random = new Random();
        private int _seed;

        public NestedDataGridGroup(string name)
        {
            Name = name;
            Items = new ObservableCollection<ChangeItem>();

            AddRowCommand = new RelayCommand(_ => AddRow());
            RemoveRowCommand = new RelayCommand(_ => RemoveRow(), _ => Items.Count > 0);

            Items.CollectionChanged += (_, __) => RemoveRowCommand.RaiseCanExecuteChanged();
        }

        public string Name { get; }

        public ObservableCollection<ChangeItem> Items { get; }

        public RelayCommand AddRowCommand { get; }

        public RelayCommand RemoveRowCommand { get; }

        public void AddRow()
        {
            var id = ++_seed;
            Items.Add(new ChangeItem
            {
                Id = id,
                Name = $"Row {id}",
                Value = _random.Next(1, 100),
            });
        }

        private void RemoveRow()
        {
            if (Items.Count == 0)
            {
                return;
            }

            Items.RemoveAt(Items.Count - 1);
        }
    }
}
