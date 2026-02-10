using System.Collections.ObjectModel;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ButtonColumnBindingsViewModel : ObservableObject
    {
        private string _lastActionMessage = "No actions yet.";

        public ButtonColumnBindingsViewModel()
        {
            Items = new ObservableCollection<ButtonColumnBindingsItem>
            {
                new() { Name = "Alpha", ActionLabel = "Run", ClickCount = 0 },
                new() { Name = "Beta", ActionLabel = "Run", ClickCount = 2 },
                new() { Name = "Gamma", ActionLabel = "Run", ClickCount = 5 }
            };

            RunActionCommand = new RelayCommand(RunAction, CanRunAction);
            ClearClicksCommand = new RelayCommand(ClearClicks, CanClearClicks);
        }

        public ObservableCollection<ButtonColumnBindingsItem> Items { get; }

        public RelayCommand RunActionCommand { get; }

        public RelayCommand ClearClicksCommand { get; }

        public string LastActionMessage
        {
            get => _lastActionMessage;
            private set => SetProperty(ref _lastActionMessage, value);
        }

        private static bool CanRunAction(object? parameter) => parameter is ButtonColumnBindingsItem;

        private void RunAction(object? parameter)
        {
            if (parameter is not ButtonColumnBindingsItem item)
            {
                return;
            }

            item.ClickCount++;
            item.ActionLabel = item.ClickCount % 2 == 0 ? "Run" : "Pause";
            LastActionMessage = $"RunAction executed for {item.Name} (count: {item.ClickCount}).";
        }

        private static bool CanClearClicks(object? parameter) => parameter is ButtonColumnBindingsItem;

        private void ClearClicks(object? parameter)
        {
            if (parameter is not ButtonColumnBindingsItem item)
            {
                return;
            }

            item.ClickCount = 0;
            item.ActionLabel = "Run";
            LastActionMessage = $"ClearClicks executed for {item.Name}.";
        }
    }

    public class ButtonColumnBindingsItem : ObservableObject
    {
        private string _name = string.Empty;
        private string _actionLabel = string.Empty;
        private int _clickCount;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string ActionLabel
        {
            get => _actionLabel;
            set => SetProperty(ref _actionLabel, value);
        }

        public int ClickCount
        {
            get => _clickCount;
            set => SetProperty(ref _clickCount, value);
        }
    }
}
