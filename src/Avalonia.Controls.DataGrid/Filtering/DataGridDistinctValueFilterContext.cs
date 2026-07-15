// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Avalonia.Controls.DataGridFiltering;

/// <summary>
/// Maintains the values, counts, search state, and selection for an Excel-style column filter.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
sealed class DataGridDistinctValueFilterContext : IFilterDistinctValuesContext, INotifyPropertyChanged
{
    private static readonly object s_nullKey = new();

    private readonly IFilteringModel _filteringModel;
    private readonly object _columnId;
    private readonly string? _propertyPath;
    private readonly IDataGridColumnValueAccessor _valueAccessor;
    private readonly IEqualityComparer _valueComparer;
    private readonly bool _usesCustomValueComparer;
    private readonly Func<object?, string> _displayFormatter;
    private readonly List<DataGridDistinctValueFilterOption> _allOptions = new();
    private object _activeDescriptorColumnId;
    private string? _searchText;
    private bool _suppressFilterUpdates;

    /// <summary>
    /// Initializes a new distinct-value filter context.
    /// </summary>
    /// <param name="filteringModel">The central filtering model updated by value selections.</param>
    /// <param name="columnId">The stable identifier of the target column.</param>
    /// <param name="valueAccessor">The reflection-free accessor used to read column values.</param>
    /// <param name="label">The label displayed above the available values.</param>
    /// <param name="propertyPath">The optional property path stored in the generated descriptor.</param>
    /// <param name="valueComparer">The optional comparer used to group and select values.</param>
    /// <param name="displayFormatter">The optional formatter used to display values.</param>
    public DataGridDistinctValueFilterContext(
        IFilteringModel filteringModel,
        object columnId,
        IDataGridColumnValueAccessor valueAccessor,
        string label,
        string? propertyPath = null,
        IEqualityComparer? valueComparer = null,
        Func<object?, string>? displayFormatter = null)
    {
        _filteringModel = filteringModel ?? throw new ArgumentNullException(nameof(filteringModel));
        _columnId = columnId ?? throw new ArgumentNullException(nameof(columnId));
        _activeDescriptorColumnId = _columnId;
        _valueAccessor = valueAccessor ?? throw new ArgumentNullException(nameof(valueAccessor));
        _propertyPath = propertyPath;
        _usesCustomValueComparer = valueComparer != null;
        _valueComparer = valueComparer ?? EqualityComparer<object>.Default;
        _displayFormatter = displayFormatter ?? FormatDisplayValue;
        Label = label ?? string.Empty;
        Options = new ObservableCollection<IFilterDistinctValueOption>();
    }

    /// <summary>
    /// Raised when a bindable property changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the label displayed above the filter values.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Gets or sets the case-insensitive substring used to search value display text.
    /// </summary>
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (string.Equals(_searchText, value, StringComparison.Ordinal))
            {
                return;
            }

            _searchText = value;
            OnPropertyChanged();
            ApplySearch();
        }
    }

    /// <summary>
    /// Gets the value options currently visible after applying <see cref="SearchText"/>.
    /// </summary>
    public ObservableCollection<IFilterDistinctValueOption> Options { get; }

    /// <summary>
    /// Rebuilds distinct values and counts from the supplied unfiltered source.
    /// </summary>
    /// <param name="items">The source rows to inspect.</param>
    public void Refresh(IEnumerable? items)
    {
        var counts = new Dictionary<object, ValueCount>(new NullKeyComparer(_valueComparer));
        if (items != null)
        {
            foreach (object? item in items)
            {
                object? value = _valueAccessor.GetValue(item!);
                object key = value ?? s_nullKey;
                if (counts.TryGetValue(key, out ValueCount? entry))
                {
                    entry.Count++;
                }
                else
                {
                    counts.Add(key, new ValueCount(value));
                }
            }
        }

        FilteringDescriptor? activeDescriptor = FindActiveDescriptor();
        _activeDescriptorColumnId = activeDescriptor?.ColumnId ?? _columnId;
        IReadOnlyList<object>? selectedValues = activeDescriptor?.Operator == FilteringOperator.In
            ? activeDescriptor.Values
            : null;

        if (selectedValues != null)
        {
            for (int i = 0; i < selectedValues.Count; i++)
            {
                object? selectedValue = selectedValues[i];
                object key = selectedValue ?? s_nullKey;
                if (!counts.ContainsKey(key))
                {
                    counts.Add(key, new ValueCount(selectedValue, count: 0));
                }
            }
        }

        var nextOptions = new List<DataGridDistinctValueFilterOption>(counts.Count);
        foreach (ValueCount entry in counts.Values)
        {
            bool isSelected = Contains(selectedValues, entry.Value);
            nextOptions.Add(new DataGridDistinctValueFilterOption(
                entry.Value,
                _displayFormatter(entry.Value),
                entry.Count,
                isSelected,
                OnOptionSelectionChanged));
        }

        nextOptions.Sort(static (left, right) =>
            StringComparer.CurrentCultureIgnoreCase.Compare(left.Display, right.Display));

        _suppressFilterUpdates = true;
        try
        {
            _allOptions.Clear();
            _allOptions.AddRange(nextOptions);
            ApplySearch();
        }
        finally
        {
            _suppressFilterUpdates = false;
        }
    }

    private void OnOptionSelectionChanged()
    {
        if (_suppressFilterUpdates)
        {
            return;
        }

        var selectedValues = new List<object>();
        for (int i = 0; i < _allOptions.Count; i++)
        {
            DataGridDistinctValueFilterOption option = _allOptions[i];
            if (option.IsSelected)
            {
                selectedValues.Add(option.Value!);
            }
        }

        if (selectedValues.Count == 0)
        {
            _filteringModel.Remove(_activeDescriptorColumnId);
            _activeDescriptorColumnId = _columnId;
            return;
        }

        Func<object, bool>? predicate = _usesCustomValueComparer
            ? item => Contains(selectedValues, _valueAccessor.GetValue(item))
            : null;
        _filteringModel.SetOrUpdate(new FilteringDescriptor(
            columnId: _activeDescriptorColumnId,
            @operator: FilteringOperator.In,
            propertyPath: _propertyPath,
            values: selectedValues,
            predicate: predicate));
    }

    private void ApplySearch()
    {
        Options.Clear();
        string? search = string.IsNullOrWhiteSpace(_searchText) ? null : _searchText.Trim();
        for (int i = 0; i < _allOptions.Count; i++)
        {
            DataGridDistinctValueFilterOption option = _allOptions[i];
            if (search == null || option.Display.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0)
            {
                Options.Add(option);
            }
        }
    }

    private FilteringDescriptor? FindActiveDescriptor()
    {
        IReadOnlyList<FilteringDescriptor> descriptors = _filteringModel.Descriptors;
        for (int i = 0; i < descriptors.Count; i++)
        {
            FilteringDescriptor descriptor = descriptors[i];
            if (Equals(descriptor.ColumnId, _columnId))
            {
                return descriptor;
            }
        }

        if (!string.IsNullOrEmpty(_propertyPath))
        {
            for (int i = 0; i < descriptors.Count; i++)
            {
                FilteringDescriptor descriptor = descriptors[i];
                if (string.Equals(descriptor.PropertyPath, _propertyPath, StringComparison.Ordinal))
                {
                    return descriptor;
                }
            }
        }

        return null;
    }

    private bool Contains(IReadOnlyList<object>? values, object? candidate)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (_valueComparer.Equals(values[i], candidate!))
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatDisplayValue(object? value)
    {
        if (value == null)
        {
            return "(Empty)";
        }

        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class ValueCount
    {
        public ValueCount(object? value, int count = 1)
        {
            Value = value;
            Count = count;
        }

        public object? Value { get; }

        public int Count { get; set; }
    }

    private sealed class NullKeyComparer : IEqualityComparer<object>
    {
        private readonly IEqualityComparer _inner;

        public NullKeyComparer(IEqualityComparer inner)
        {
            _inner = inner;
        }

        bool IEqualityComparer<object>.Equals(object? left, object? right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (ReferenceEquals(left, s_nullKey) || ReferenceEquals(right, s_nullKey))
            {
                return false;
            }

            return _inner.Equals(left!, right!);
        }

        int IEqualityComparer<object>.GetHashCode(object value)
        {
            return ReferenceEquals(value, s_nullKey) ? 0 : _inner.GetHashCode(value);
        }
    }
}

/// <summary>
/// Represents one distinct source value and its count.
/// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
sealed class DataGridDistinctValueFilterOption : IFilterDistinctValueOption, INotifyPropertyChanged
{
    private readonly Action _selectionChanged;
    private bool _isSelected;

    internal DataGridDistinctValueFilterOption(
        object? value,
        string display,
        int count,
        bool isSelected,
        Action selectionChanged)
    {
        Value = value;
        Display = display;
        Count = count;
        _isSelected = isSelected;
        _selectionChanged = selectionChanged;
    }

    /// <summary>
    /// Raised when <see cref="IsSelected"/> changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the source value represented by this option.
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Gets the formatted value displayed in the popup.
    /// </summary>
    public string Display { get; }

    /// <summary>
    /// Gets the number of source rows containing this value.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets or sets whether the value participates in the active filter.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            _selectionChanged();
        }
    }
}
