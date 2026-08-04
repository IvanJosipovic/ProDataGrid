// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Avalonia.Controls.DataGridFiltering;

/// <summary>
/// Minimal contract for text-based filter contexts consumed by the shared filter templates.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
interface IFilterTextContext
{
    string Label { get; }
    string? Text { get; set; }
    ICommand ApplyCommand { get; }
    ICommand ClearCommand { get; }
}

/// <summary>
/// Minimal contract for numeric range filter contexts.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
interface IFilterNumberContext
{
    string Label { get; }
    double Minimum { get; }
    double Maximum { get; }
    double? MinValue { get; set; }
    double? MaxValue { get; set; }
    ICommand ApplyCommand { get; }
    ICommand ClearCommand { get; }
}

/// <summary>
/// Minimal contract for date range filter contexts.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
interface IFilterDateContext
{
    string Label { get; }
    System.DateTimeOffset? From { get; set; }
    System.DateTimeOffset? To { get; set; }
    ICommand ApplyCommand { get; }
    ICommand ClearCommand { get; }
}

/// <summary>
/// Minimal contract for enum/multi-select filter contexts.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
interface IFilterEnumContext
{
    string Label { get; }
    ObservableCollection<IEnumOption> Options { get; }
    ICommand ApplyCommand { get; }
    ICommand ClearCommand { get; }
}

/// <summary>
/// Option contract for enum/multi-select filter items.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
interface IEnumOption
{
    string Display { get; }
    bool IsSelected { get; set; }
}

/// <summary>
/// Contract consumed by the built-in distinct-value filter template.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
interface IFilterDistinctValuesContext
{
    /// <summary>
    /// Gets the label displayed above the filter values.
    /// </summary>
    string Label { get; }

    /// <summary>
    /// Gets or sets the substring used to search the available values.
    /// </summary>
    string? SearchText { get; set; }

    /// <summary>
    /// Gets the values currently visible after applying <see cref="SearchText"/>.
    /// </summary>
    ObservableCollection<IFilterDistinctValueOption> Options { get; }
}

/// <summary>
/// Represents one value in a distinct-value filter.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
interface IFilterDistinctValueOption
{
    /// <summary>
    /// Gets the display text for the value.
    /// </summary>
    string Display { get; }

    /// <summary>
    /// Gets the number of source rows containing the value.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets or sets whether the value participates in the active filter.
    /// </summary>
    bool IsSelected { get; set; }
}
