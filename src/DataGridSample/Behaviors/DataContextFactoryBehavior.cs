using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace DataGridSample.Behaviors;

public interface IDataContextFactory
{
    object Create();
}

public sealed class DataContextFactoryBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<IDataContextFactory?> FactoryProperty =
        AvaloniaProperty.RegisterAttached<DataContextFactoryBehavior, Control, IDataContextFactory?>(
            "Factory");

    private static readonly AttachedProperty<FactorySubscription?> SubscriptionProperty =
        AvaloniaProperty.RegisterAttached<DataContextFactoryBehavior, Control, FactorySubscription?>(
            "Subscription");

    static DataContextFactoryBehavior()
    {
        FactoryProperty.Changed.AddClassHandler<Control>(OnFactoryChanged);
    }

    private DataContextFactoryBehavior()
    {
    }

    public static IDataContextFactory? GetFactory(Control control) =>
        control.GetValue(FactoryProperty);

    public static void SetFactory(Control control, IDataContextFactory? value) =>
        control.SetValue(FactoryProperty, value);

    private static void OnFactoryChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.GetValue(SubscriptionProperty)?.Dispose();
        control.ClearValue(SubscriptionProperty);

        if (e.NewValue is IDataContextFactory factory)
        {
            control.SetValue(SubscriptionProperty, new FactorySubscription(control, factory));
        }
    }

    private sealed class FactorySubscription : IDisposable
    {
        private Control? _control;
        private readonly IDataContextFactory _factory;

        public FactorySubscription(Control control, IDataContextFactory factory)
        {
            _control = control;
            _factory = factory;
            control.AttachedToVisualTree += OnAttachedToVisualTree;

            if (control.IsAttachedToVisualTree())
            {
                EnsureDataContext(control);
            }
        }

        public void Dispose()
        {
            if (_control is not { } control)
            {
                return;
            }

            control.AttachedToVisualTree -= OnAttachedToVisualTree;
            _control = null;
        }

        private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (sender is Control control)
            {
                EnsureDataContext(control);
            }
        }

        private void EnsureDataContext(Control control)
        {
            control.DataContext ??= _factory.Create();
        }
    }
}
