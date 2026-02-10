using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ToggleSwitchColumnDefinitionBindingsViewModel : ObservableObject
    {
        private string _lastToggleMessage = "No toggles yet.";

        public ToggleSwitchColumnDefinitionBindingsViewModel()
        {
            Items = new ObservableCollection<ToggleSwitchColumnDefinitionBindingsItem>
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

            foreach (ToggleSwitchColumnDefinitionBindingsItem item in Items)
            {
                item.PropertyChanged += OnItemPropertyChanged;
            }

            ColumnDefinitions = new ObservableCollection<DataGridColumnDefinition>
            {
                new DataGridTextColumnDefinition
                {
                    Header = "Name",
                    Binding = CreateBinding(nameof(ToggleSwitchColumnDefinitionBindingsItem.Name), item => item.Name),
                    IsReadOnly = true
                },
                new DataGridToggleSwitchColumnDefinition
                {
                    Header = "Bound Labels",
                    Binding = CreateBinding(
                        nameof(ToggleSwitchColumnDefinitionBindingsItem.IsOnline),
                        item => item.IsOnline,
                        (item, value) => item.IsOnline = value),
                    OnContent = new Binding(nameof(ToggleSwitchColumnDefinitionBindingsItem.OnLabel)),
                    OffContent = new Binding(nameof(ToggleSwitchColumnDefinitionBindingsItem.OffLabel))
                },
                new DataGridToggleSwitchColumnDefinition
                {
                    Header = "Static Labels",
                    Binding = CreateBinding(
                        nameof(ToggleSwitchColumnDefinitionBindingsItem.IsPinned),
                        item => item.IsPinned,
                        (item, value) => item.IsPinned = value),
                    OnContent = "Pinned",
                    OffContent = "Unpinned"
                }
            };
        }

        public ObservableCollection<ToggleSwitchColumnDefinitionBindingsItem> Items { get; }

        public ObservableCollection<DataGridColumnDefinition> ColumnDefinitions { get; }

        public string LastToggleMessage
        {
            get => _lastToggleMessage;
            private set => SetProperty(ref _lastToggleMessage, value);
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not ToggleSwitchColumnDefinitionBindingsItem item)
            {
                return;
            }

            if (e.PropertyName == nameof(ToggleSwitchColumnDefinitionBindingsItem.IsOnline))
            {
                LastToggleMessage = $"Online toggled for {item.Name}: {(item.IsOnline ? "On" : "Off")}.";
                return;
            }

            if (e.PropertyName == nameof(ToggleSwitchColumnDefinitionBindingsItem.IsPinned))
            {
                LastToggleMessage = $"Pinned toggled for {item.Name}: {(item.IsPinned ? "On" : "Off")}.";
            }
        }

        private static DataGridBindingDefinition CreateBinding<TValue>(
            string name,
            Func<ToggleSwitchColumnDefinitionBindingsItem, TValue> getter,
            Action<ToggleSwitchColumnDefinitionBindingsItem, TValue>? setter = null)
        {
            var propertyInfo = new ClrPropertyInfo(
                name,
                target => target is ToggleSwitchColumnDefinitionBindingsItem item ? getter(item) : default,
                setter == null
                    ? null
                    : (target, value) =>
                    {
                        if (target is ToggleSwitchColumnDefinitionBindingsItem item)
                        {
                            TValue typedValue = value is TValue castValue ? castValue : default!;
                            setter(item, typedValue);
                        }
                    },
                typeof(TValue));

            return DataGridBindingDefinition.Create<ToggleSwitchColumnDefinitionBindingsItem, TValue>(propertyInfo, getter, setter);
        }
    }

    public class ToggleSwitchColumnDefinitionBindingsItem : ObservableObject
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
