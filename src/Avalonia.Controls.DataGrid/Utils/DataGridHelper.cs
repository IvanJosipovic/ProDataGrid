namespace Avalonia.Controls
{
    internal static class DataGridHelper
    {
        internal static void SyncColumnProperty<T>(AvaloniaObject column, AvaloniaObject content, AvaloniaProperty<T> property)
        {
            SyncColumnProperty(column, content, property, property);
        }

        internal static void SyncColumnProperty<T>(AvaloniaObject column, AvaloniaObject content, AvaloniaProperty<T> contentProperty, AvaloniaProperty<T> columnProperty)
        {
            SyncColumnProperty(column, content, contentProperty, columnProperty, clearUnsetValue: true);
        }

        internal static void SyncColumnProperty<T>(
            AvaloniaObject column,
            AvaloniaObject content,
            AvaloniaProperty<T> contentProperty,
            AvaloniaProperty<T> columnProperty,
            bool clearUnsetValue)
        {
            if (!column.IsSet(columnProperty))
            {
                // Newly-created and same-column recycled cell content already inherits the
                // property default/theme value. Avoid entering the property store merely to
                // clear a value that is not present; custom-drawing cells synchronize many
                // optional properties for every realization.
                if (clearUnsetValue && content.IsSet(contentProperty))
                {
                    content.ClearValue(contentProperty);
                }
            }
            else
            {
                content.SetValue(contentProperty, column.GetValue(columnProperty));
            }
        }
    }
}
