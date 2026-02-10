using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ToggleButtonColumnDefinitionBindingsViewModel : ObservableObject
    {
        private string _lastToggleMessage = "No toggles yet.";

        public ToggleButtonColumnDefinitionBindingsViewModel()
        {
            Items = new ObservableCollection<ToggleButtonColumnDefinitionBindingsItem>
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

            foreach (ToggleButtonColumnDefinitionBindingsItem item in Items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }

            ColumnDefinitions = new ObservableCollection<DataGridColumnDefinition>
            {
                new DataGridTextColumnDefinition
                {
                    Header = "Name",
                    Binding = CreateBinding(nameof(ToggleButtonColumnDefinitionBindingsItem.Name), item => item.Name),
                    IsReadOnly = true
                },
                new DataGridToggleButtonColumnDefinition
                {
                    Header = "Bound Content",
                    Binding = CreateBinding(
                        nameof(ToggleButtonColumnDefinitionBindingsItem.IsFavorite),
                        item => item.IsFavorite,
                        (item, value) => item.IsFavorite = value),
                    Content = new Binding(nameof(ToggleButtonColumnDefinitionBindingsItem.ActionLabel))
                },
                new DataGridToggleButtonColumnDefinition
                {
                    Header = "Bound State Content",
                    Binding = CreateBinding(
                        nameof(ToggleButtonColumnDefinitionBindingsItem.IsEnabled),
                        item => item.IsEnabled,
                        (item, value) => item.IsEnabled = value),
                    CheckedContent = new Binding(nameof(ToggleButtonColumnDefinitionBindingsItem.CheckedLabel)),
                    UncheckedContent = new Binding(nameof(ToggleButtonColumnDefinitionBindingsItem.UncheckedLabel))
                },
                new DataGridToggleButtonColumnDefinition
                {
                    Header = "Static State Content",
                    Binding = CreateBinding(
                        nameof(ToggleButtonColumnDefinitionBindingsItem.IsArchived),
                        item => item.IsArchived,
                        (item, value) => item.IsArchived = value),
                    CheckedContent = "Archived",
                    UncheckedContent = "Active"
                }
            };
        }

        public ObservableCollection<ToggleButtonColumnDefinitionBindingsItem> Items { get; }

        public ObservableCollection<DataGridColumnDefinition> ColumnDefinitions { get; }

        public string LastToggleMessage
        {
            get => _lastToggleMessage;
            private set => SetProperty(ref _lastToggleMessage, value);
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ToggleButtonColumnDefinitionBindingsItem item)
            {
                return;
            }

            if (e.PropertyName == nameof(ToggleButtonColumnDefinitionBindingsItem.IsFavorite))
            {
                item.ActionLabel = item.IsFavorite ? "Pinned" : "Pin";
                LastToggleMessage = $"Favorite toggled for {item.Name}: {(item.IsFavorite ? "On" : "Off")}.";
                return;
            }

            if (e.PropertyName == nameof(ToggleButtonColumnDefinitionBindingsItem.IsEnabled))
            {
                LastToggleMessage = $"Enabled toggled for {item.Name}: {(item.IsEnabled ? "On" : "Off")}.";
                return;
            }

            if (e.PropertyName == nameof(ToggleButtonColumnDefinitionBindingsItem.IsArchived))
            {
                LastToggleMessage = $"Archived toggled for {item.Name}: {(item.IsArchived ? "On" : "Off")}.";
            }
        }

        private static DataGridBindingDefinition CreateBinding<TValue>(
            string name,
            Func<ToggleButtonColumnDefinitionBindingsItem, TValue> getter,
            Action<ToggleButtonColumnDefinitionBindingsItem, TValue>? setter = null)
        {
            var propertyInfo = new ClrPropertyInfo(
                name,
                target => target is ToggleButtonColumnDefinitionBindingsItem item ? getter(item) : default,
                setter == null
                    ? null
                    : (target, value) =>
                    {
                        if (target is ToggleButtonColumnDefinitionBindingsItem item)
                        {
                            TValue typedValue = value is TValue castValue ? castValue : default!;
                            setter(item, typedValue);
                        }
                    },
                typeof(TValue));

            return DataGridBindingDefinition.Create<ToggleButtonColumnDefinitionBindingsItem, TValue>(propertyInfo, getter, setter);
        }
    }

    public class ToggleButtonColumnDefinitionBindingsItem : ObservableObject
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
