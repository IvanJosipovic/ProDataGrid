using System.Collections.ObjectModel;
using System.Windows.Input;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class EditableViewModel
    {
        public EditableViewModel()
        {
            EditablePeople = new ObservableCollection<Person>
            {
                new Person { FirstName = "John", LastName = "Doe" , Age = 30},
                new Person { FirstName = "Elizabeth", LastName = "Thomas", IsBanned = true , Age = 40 },
                new Person { FirstName = "Zack", LastName = "Ward" , Age = 50 }
            };

            AddPersonCommand = new RelayCommand(_ => EditablePeople.Add(new Person()));
        }

        public ObservableCollection<Person> EditablePeople { get; }

        public ICommand AddPersonCommand { get; }
    }
}
