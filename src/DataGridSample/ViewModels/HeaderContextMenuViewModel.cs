// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls.DataGridFiltering;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels;

public sealed class HeaderContextMenuViewModel : ObservableObject
{
    private readonly RelayCommand _clearAllCommand;

    public HeaderContextMenuViewModel()
    {
        Items = new ObservableCollection<Person>(CreatePeople());
        View = new DataGridCollectionView(Items);
        FilteringModel = new FilteringModel();

        NameFilter = new TextFilterContext(
            "First name contains",
            apply: ApplyNameFilter,
            clear: () => ClearFilter(nameof(Person.FirstName), () => NameFilter.Text = string.Empty));

        StatusFilter = new EnumFilterContext(
            "Status (In)",
            Enum.GetNames(typeof(PersonStatus)),
            apply: ApplyStatusFilter,
            clear: () => ClearFilter(nameof(Person.Status), () => StatusFilter.SelectNone()));

        _clearAllCommand = new RelayCommand(_ => FilteringModel.Clear(), _ => FilteringModel.Descriptors.Count > 0);
        ClearAllCommand = _clearAllCommand;
        FilteringModel.FilteringChanged += (_, __) => _clearAllCommand.RaiseCanExecuteChanged();

        ShowFilterFlyoutCommand = new RelayCommand(
            parameter => RequestShowFilterFlyout(parameter));

        ClearColumnFilterCommand = new RelayCommand(
            parameter => ClearFilterForColumnId(parameter));

    }

    public ObservableCollection<Person> Items { get; }

    public DataGridCollectionView View { get; }

    public FilteringModel FilteringModel { get; }

    public ICommand ClearAllCommand { get; }

    public ICommand ShowFilterFlyoutCommand { get; }

    public ICommand ClearColumnFilterCommand { get; }


    public TextFilterContext NameFilter { get; }

    public EnumFilterContext StatusFilter { get; }


    private void ApplyNameFilter(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            FilteringModel.Remove(nameof(Person.FirstName));
            return;
        }

        FilteringModel.SetOrUpdate(new FilteringDescriptor(
            columnId: nameof(Person.FirstName),
            @operator: FilteringOperator.Contains,
            propertyPath: nameof(Person.FirstName),
            value: text,
            stringComparison: StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyStatusFilter(IReadOnlyList<string> selected)
    {
        if (selected.Count == 0)
        {
            FilteringModel.Remove(nameof(Person.Status));
            return;
        }

        var values = selected
            .Select(value => Enum.Parse<PersonStatus>(value))
            .Cast<object>()
            .ToArray();

        FilteringModel.SetOrUpdate(new FilteringDescriptor(
            columnId: nameof(Person.Status),
            @operator: FilteringOperator.In,
            propertyPath: nameof(Person.Status),
            values: values));
    }

    private void ClearFilter(string columnId, Action reset)
    {
        reset();
        FilteringModel.Remove(columnId);
    }

    private void RequestShowFilterFlyout(object? columnId)
    {
        if (columnId == null)
        {
            return;
        }

        if (FilteringModel is IFilteringModelInteraction interaction)
        {
            interaction.RequestShowFilterFlyout(columnId);
        }
    }

    private void ClearFilterForColumnId(object? columnId)
    {
        if (columnId is not string columnKey || string.IsNullOrWhiteSpace(columnKey))
        {
            return;
        }

        if (string.Equals(columnKey, nameof(Person.FirstName), StringComparison.Ordinal))
        {
            ClearFilter(nameof(Person.FirstName), () => NameFilter.Text = string.Empty);
            return;
        }

        if (string.Equals(columnKey, nameof(Person.Status), StringComparison.Ordinal))
        {
            ClearFilter(nameof(Person.Status), () => StatusFilter.SelectNone());
            return;
        }

        FilteringModel.Remove(columnKey);
    }


    private static IEnumerable<Person> CreatePeople()
    {
        return new[]
        {
            new Person { FirstName = "Ada", LastName = "Lovelace", Age = 36, Status = PersonStatus.Active },
            new Person { FirstName = "Alan", LastName = "Turing", Age = 41, Status = PersonStatus.Suspended },
            new Person { FirstName = "Grace", LastName = "Hopper", Age = 85, Status = PersonStatus.Active },
            new Person { FirstName = "Edsger", LastName = "Dijkstra", Age = 72, Status = PersonStatus.Disabled },
            new Person { FirstName = "Barbara", LastName = "Liskov", Age = 84, Status = PersonStatus.Active },
            new Person { FirstName = "Donald", LastName = "Knuth", Age = 86, Status = PersonStatus.New }
        };
    }
}
