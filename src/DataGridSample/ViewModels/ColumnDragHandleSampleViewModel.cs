using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ColumnDragHandleSampleViewModel : ObservableObject
    {
        private DataGridColumnDragHandle _dragHandle = DataGridColumnDragHandle.DragHandle;
        private bool _showHandle = true;
        private bool _canUserReorderColumns = true;

        public ColumnDragHandleSampleViewModel()
        {
            Items = new ObservableCollection<Person>
            {
                new() { FirstName = "Ada", LastName = "Lovelace", Age = 36, Status = PersonStatus.Active },
                new() { FirstName = "Alan", LastName = "Turing", Age = 41, Status = PersonStatus.New },
                new() { FirstName = "Grace", LastName = "Hopper", Age = 85, Status = PersonStatus.Suspended },
                new() { FirstName = "Jean", LastName = "Bartik", Age = 86, Status = PersonStatus.Active },
                new() { FirstName = "Claude", LastName = "Shannon", Age = 84, Status = PersonStatus.Disabled }
            };

            DragHandles = new[]
            {
                DataGridColumnDragHandle.ColumnHeader,
                DataGridColumnDragHandle.DragHandle
            };
        }

        public ObservableCollection<Person> Items { get; }

        public IReadOnlyList<DataGridColumnDragHandle> DragHandles { get; }

        public DataGridColumnDragHandle DragHandle
        {
            get => _dragHandle;
            set => SetProperty(ref _dragHandle, value);
        }

        public bool ShowHandle
        {
            get => _showHandle;
            set => SetProperty(ref _showHandle, value);
        }

        public bool CanUserReorderColumns
        {
            get => _canUserReorderColumns;
            set => SetProperty(ref _canUserReorderColumns, value);
        }
    }
}
