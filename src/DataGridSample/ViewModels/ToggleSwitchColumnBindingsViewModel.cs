using System.Collections.ObjectModel;
using System.ComponentModel;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ToggleSwitchColumnBindingsViewModel : ObservableObject
    {
        private string _lastToggleMessage = "No toggles yet.";

        public ToggleSwitchColumnBindingsViewModel()
        {
            Items = new ObservableCollection<ToggleSwitchColumnBindingsItem>
            {
                new()
                {
                    Name = "Alpha",
                    IsOnline = true,
                    IsPinned = false,
                    OnLabel = "Online",
                    OffLabel = "Offline"
                },
                new()
                {
                    Name = "Beta",
                    IsOnline = false,
                    IsPinned = true,
                    OnLabel = "Online",
                    OffLabel = "Offline"
                },
                new()
                {
                    Name = "Gamma",
                    IsOnline = true,
                    IsPinned = true,
                    OnLabel = "Online",
                    OffLabel = "Offline"
                }
            };

            foreach (ToggleSwitchColumnBindingsItem item in Items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        public ObservableCollection<ToggleSwitchColumnBindingsItem> Items { get; }

        public string LastToggleMessage
        {
            get => _lastToggleMessage;
            private set => SetProperty(ref _lastToggleMessage, value);
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ToggleSwitchColumnBindingsItem item)
            {
                return;
            }

            if (e.PropertyName == nameof(ToggleSwitchColumnBindingsItem.IsOnline))
            {
                LastToggleMessage = $"Online toggled for {item.Name}: {(item.IsOnline ? "On" : "Off")}.";
                return;
            }

            if (e.PropertyName == nameof(ToggleSwitchColumnBindingsItem.IsPinned))
            {
                LastToggleMessage = $"Pinned toggled for {item.Name}: {(item.IsPinned ? "On" : "Off")}.";
            }
        }
    }

    public class ToggleSwitchColumnBindingsItem : ObservableObject
    {
        private string _name = string.Empty;
        private bool _isOnline;
        private bool _isPinned;
        private string _onLabel = string.Empty;
        private string _offLabel = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsOnline
        {
            get => _isOnline;
            set => SetProperty(ref _isOnline, value);
        }

        public bool IsPinned
        {
            get => _isPinned;
            set => SetProperty(ref _isPinned, value);
        }

        public string OnLabel
        {
            get => _onLabel;
            set => SetProperty(ref _onLabel, value);
        }

        public string OffLabel
        {
            get => _offLabel;
            set => SetProperty(ref _offLabel, value);
        }
    }
}
