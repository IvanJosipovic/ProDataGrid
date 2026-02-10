using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ButtonColumnDefinitionBindingsViewModel : ObservableObject
    {
        private string _lastActionMessage = "No actions yet.";

        public ButtonColumnDefinitionBindingsViewModel()
        {
            RunActionCommand = new RelayCommand(RunAction, CanRunAction);
            ClearClicksCommand = new RelayCommand(ClearClicks, CanClearClicks);

            Items = new ObservableCollection<ButtonColumnDefinitionBindingsItem>
            {
                new() { Name = "Alpha", ActionLabel = "Run", ClickCount = 0 },
                new() { Name = "Beta", ActionLabel = "Run", ClickCount = 2 },
                new() { Name = "Gamma", ActionLabel = "Run", ClickCount = 5 }
            };

            ColumnDefinitions = new ObservableCollection<DataGridColumnDefinition>
            {
                new DataGridTextColumnDefinition
                {
                    Header = "Name",
                    Binding = CreateBinding(nameof(ButtonColumnDefinitionBindingsItem.Name), item => item.Name),
                    IsReadOnly = true
                },
                new DataGridTextColumnDefinition
                {
                    Header = "Clicks",
                    Binding = CreateBinding(nameof(ButtonColumnDefinitionBindingsItem.ClickCount), item => item.ClickCount),
                    IsReadOnly = true
                },
                new DataGridButtonColumnDefinition
                {
                    Header = "Bound Action",
                    Content = new Binding(nameof(ButtonColumnDefinitionBindingsItem.ActionLabel)),
                    Command = RunActionCommand,
                    CommandParameter = new Binding(".")
                },
                new DataGridButtonColumnDefinition
                {
                    Header = "Fallback Parameter",
                    Content = "Clear Clicks",
                    Command = ClearClicksCommand
                }
            };
        }

        public ObservableCollection<ButtonColumnDefinitionBindingsItem> Items { get; }

        public ObservableCollection<DataGridColumnDefinition> ColumnDefinitions { get; }

        public RelayCommand RunActionCommand { get; }

        public RelayCommand ClearClicksCommand { get; }

        public string LastActionMessage
        {
            get => _lastActionMessage;
            private set => SetProperty(ref _lastActionMessage, value);
        }

        private static bool CanRunAction(object? parameter) => parameter is ButtonColumnDefinitionBindingsItem;

        private void RunAction(object? parameter)
        {
            if (parameter is not ButtonColumnDefinitionBindingsItem item)
            {
                return;
            }

            item.ClickCount++;
            item.ActionLabel = item.ClickCount % 2 == 0 ? "Run" : "Pause";
            LastActionMessage = $"RunAction executed for {item.Name} (count: {item.ClickCount}).";
        }

        private static bool CanClearClicks(object? parameter) => parameter is ButtonColumnDefinitionBindingsItem;

        private void ClearClicks(object? parameter)
        {
            if (parameter is not ButtonColumnDefinitionBindingsItem item)
            {
                return;
            }

            item.ClickCount = 0;
            item.ActionLabel = "Run";
            LastActionMessage = $"ClearClicks executed for {item.Name}.";
        }

        private static DataGridBindingDefinition CreateBinding<TValue>(
            string name,
            Func<ButtonColumnDefinitionBindingsItem, TValue> getter,
            Action<ButtonColumnDefinitionBindingsItem, TValue>? setter = null)
        {
            var propertyInfo = new ClrPropertyInfo(
                name,
                target => target is ButtonColumnDefinitionBindingsItem item ? getter(item) : default,
                setter == null
                    ? null
                    : (target, value) =>
                    {
                        if (target is ButtonColumnDefinitionBindingsItem item)
                        {
                            TValue typedValue = value is TValue castValue ? castValue : default!;
                            setter(item, typedValue);
                        }
                    },
                typeof(TValue));

            return DataGridBindingDefinition.Create<ButtonColumnDefinitionBindingsItem, TValue>(propertyInfo, getter, setter);
        }
    }

    public class ButtonColumnDefinitionBindingsItem : ObservableObject
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
