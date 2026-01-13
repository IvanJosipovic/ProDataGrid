using System.Collections.ObjectModel;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ColumnDragIndicatorHeaderBindingViewModel : ObservableObject
    {
        private string _lockStateColumnHeader = "Lock state";

        public ColumnDragIndicatorHeaderBindingViewModel()
        {
            Items = new ObservableCollection<Person>
            {
                new() { FirstName = "Ada", LastName = "Lovelace", Age = 36, IsBanned = false },
                new() { FirstName = "Alan", LastName = "Turing", Age = 41, IsBanned = true },
                new() { FirstName = "Grace", LastName = "Hopper", Age = 85, IsBanned = false },
                new() { FirstName = "Jean", LastName = "Bartik", Age = 86, IsBanned = true },
                new() { FirstName = "Claude", LastName = "Shannon", Age = 84, IsBanned = false }
            };
        }

        public ObservableCollection<Person> Items { get; }

        public string LockStateColumnHeader
        {
            get => _lockStateColumnHeader;
            set => SetProperty(ref _lockStateColumnHeader, value);
        }
    }
}
