using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class AutoColumnsViewModel : ObservableObject
    {
        private int _itemCount = 50;

        public AutoColumnsViewModel()
        {
            RegenerateCommand = new RelayCommand(_ => Populate());
            Populate();
        }

        public ObservableCollection<Person> Items { get; } = new();

        public int ItemCount
        {
            get => _itemCount;
            set => SetProperty(ref _itemCount, value);
        }

        public ICommand RegenerateCommand { get; }

        private void Populate()
        {
            Items.Clear();
            var random = new Random(23);

            string[] firstNames = { "Alex", "Sam", "Jordan", "Taylor", "Morgan", "Jamie", "Casey", "Riley", "Avery", "Skyler" };
            string[] lastNames = { "Smith", "Johnson", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor", "Anderson", "Thomas" };
            var statuses = Enum.GetValues<PersonStatus>();

            for (int i = 0; i < ItemCount; i++)
            {
                var person = new Person
                {
                    FirstName = firstNames[random.Next(firstNames.Length)],
                    LastName = lastNames[random.Next(lastNames.Length)],
                    Age = random.Next(18, 75),
                    IsBanned = random.NextDouble() < 0.15,
                    Status = statuses[random.Next(statuses.Length)],
                    OptionalStatus = random.NextDouble() < 0.3 ? statuses[random.Next(statuses.Length)] : null,
                    ProfileLink = new Uri($"https://example.com/profile/{i + 1}")
                };
                Items.Add(person);
            }
        }

    }
}
