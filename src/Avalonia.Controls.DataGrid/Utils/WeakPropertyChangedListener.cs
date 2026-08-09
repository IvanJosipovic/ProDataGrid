// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.ComponentModel;

namespace Avalonia.Controls.Utils
{
    /// <summary>
    /// Provides a reusable property-change delegate that does not retain its subscriber.
    /// </summary>
    /// <typeparam name="TSubscriber">The weakly referenced subscriber type.</typeparam>
    internal sealed class WeakPropertyChangedListener<TSubscriber>
        where TSubscriber : class
    {
        private readonly WeakReference<TSubscriber> _subscriber;
        private readonly Action<TSubscriber, object?, PropertyChangedEventArgs> _callback;

        public WeakPropertyChangedListener(
            TSubscriber subscriber,
            Action<TSubscriber, object?, PropertyChangedEventArgs> callback)
        {
            _subscriber = new WeakReference<TSubscriber>(subscriber);
            _callback = callback;
            Handler = OnPropertyChanged;
        }

        public PropertyChangedEventHandler Handler { get; }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_subscriber.TryGetTarget(out TSubscriber? subscriber))
            {
                _callback(subscriber, sender, e);
            }
            else if (sender is INotifyPropertyChanged notifier)
            {
                notifier.PropertyChanged -= Handler;
            }
        }
    }
}
