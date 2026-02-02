using System;
using Avalonia.Controls;
using Avalonia.Data.Core;

namespace ProDataGrid.ExcelSample.Helpers;

internal static class ColumnDefinitionBindingFactory
{
    public static IPropertyInfo CreateProperty<TItem, TValue>(
        string name,
        Func<TItem, TValue> getter,
        Action<TItem, TValue>? setter = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Property name is required.", nameof(name));
        }

        if (getter == null)
        {
            throw new ArgumentNullException(nameof(getter));
        }

        return new ClrPropertyInfo(
            name,
            target => getter((TItem)target),
            setter == null
                ? null
                : (target, value) => setter((TItem)target, value is null ? default! : (TValue)value),
            typeof(TValue));
    }

    public static DataGridBindingDefinition CreateBinding<TItem, TValue>(
        string name,
        Func<TItem, TValue> getter,
        Action<TItem, TValue>? setter = null)
    {
        var propertyInfo = CreateProperty(name, getter, setter);
        return DataGridBindingDefinition.Create(propertyInfo, getter, setter);
    }
}
