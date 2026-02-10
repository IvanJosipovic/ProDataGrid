using System.Collections.ObjectModel;
using System.ComponentModel;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ToggleButtonColumnBindingsViewModel : ObservableObject
    {
        private string _lastToggleMessage = "No toggles yet.";

        public ToggleButtonColumnBindingsViewModel()
        {
            Items = new ObservableCollection<ToggleButtonColumnBindingsItem>
            {
                new()
                {
                    Name = "Alpha",
                    IsFavorite = true,
                    IsEnabled = false,
                    IsArchived = false,
                    ActionLabel = "Pinned",
                    CheckedLabel = "Enabled",
                    UncheckedLabel = "Disabled"
                },
                new()
                {
                    Name = "Beta",
                    IsFavorite = false,
                    IsEnabled = true,
                    IsArchived = false,
                    ActionLabel = "Pin",
                    CheckedLabel = "Enabled",
                    UncheckedLabel = "Disabled"
                },
                new()
                {
                    Name = "Gamma",
                    IsFavorite = true,
                    IsEnabled = true,
                    IsArchived = true,
                    ActionLabel = "Pinned",
                    CheckedLabel = "Enabled",
                    UncheckedLabel = "Disabled"
                }
            };

            foreach (ToggleButtonColumnBindingsItem item in Items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }
        }

        public ObservableCollection<ToggleButtonColumnBindingsItem> Items { get; }

        public string LastToggleMessage
        {
            get => _lastToggleMessage;
            private set => SetProperty(ref _lastToggleMessage, value);
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ToggleButtonColumnBindingsItem item)
            {
                return;
            }

            if (e.PropertyName == nameof(ToggleButtonColumnBindingsItem.IsFavorite))
            {
                item.ActionLabel = item.IsFavorite ? "Pinned" : "Pin";
                LastToggleMessage = $"Favorite toggled for {item.Name}: {(item.IsFavorite ? "On" : "Off")}.";
                return;
            }

            if (e.PropertyName == nameof(ToggleButtonColumnBindingsItem.IsEnabled))
            {
                LastToggleMessage = $"Enabled toggled for {item.Name}: {(item.IsEnabled ? "On" : "Off")}.";
                return;
            }

            if (e.PropertyName == nameof(ToggleButtonColumnBindingsItem.IsArchived))
            {
                LastToggleMessage = $"Archived toggled for {item.Name}: {(item.IsArchived ? "On" : "Off")}.";
            }
        }
    }

    public class ToggleButtonColumnBindingsItem : ObservableObject
    {
        private string _name = string.Empty;
        private bool _isFavorite;
        private bool _isEnabled;
        private bool _isArchived;
        private string _actionLabel = string.Empty;
        private string _checkedLabel = string.Empty;
        private string _uncheckedLabel = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool IsFavorite
        {
            get => _isFavorite;
            set => SetProperty(ref _isFavorite, value);
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        public bool IsArchived
        {
            get => _isArchived;
            set => SetProperty(ref _isArchived, value);
        }

        public string ActionLabel
        {
            get => _actionLabel;
            set => SetProperty(ref _actionLabel, value);
        }

        public string CheckedLabel
        {
            get => _checkedLabel;
            set => SetProperty(ref _checkedLabel, value);
        }

        public string UncheckedLabel
        {
            get => _uncheckedLabel;
            set => SetProperty(ref _uncheckedLabel, value);
        }
    }
}
