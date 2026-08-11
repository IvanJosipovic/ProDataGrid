// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Avalonia.Controls
{
    /// <summary>Describes one generated validation projection update.</summary>
    /// <typeparam name="TKey">The stable row key type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    readonly struct DataGridGeneratedValidationChange<TKey> : IEquatable<DataGridGeneratedValidationChange<TKey>>
    {
        /// <summary>Initializes a validation update.</summary>
        public DataGridGeneratedValidationChange(
            TKey itemKey,
            string columnKey,
            string propertyName,
            DataGridGeneratedEditResult result,
            bool hasError)
        {
            ItemKey = itemKey;
            ColumnKey = columnKey ?? throw new ArgumentNullException(nameof(columnKey));
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            Result = result;
            HasError = hasError;
        }

        /// <summary>Gets the stable row key.</summary>
        public TKey ItemKey { get; }

        /// <summary>Gets the stable generated column key.</summary>
        public string ColumnKey { get; }

        /// <summary>Gets the CLR property name used by <see cref="INotifyDataErrorInfo"/>.</summary>
        public string PropertyName { get; }

        /// <summary>Gets the structured edit result.</summary>
        public DataGridGeneratedEditResult Result { get; }

        /// <summary>Gets whether the keyed field currently has an error.</summary>
        public bool HasError { get; }

        /// <inheritdoc />
        public bool Equals(DataGridGeneratedValidationChange<TKey> other) =>
            EqualityComparer<TKey>.Default.Equals(ItemKey, other.ItemKey) &&
            string.Equals(ColumnKey, other.ColumnKey, StringComparison.Ordinal) &&
            string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal) &&
            Result.Equals(other.Result) &&
            HasError == other.HasError;

        /// <inheritdoc />
        public override bool Equals(object obj) =>
            obj is DataGridGeneratedValidationChange<TKey> other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(
            EqualityComparer<TKey>.Default.GetHashCode(ItemKey),
            StringComparer.Ordinal.GetHashCode(ColumnKey),
            StringComparer.Ordinal.GetHashCode(PropertyName),
            Result,
            HasError);

        /// <summary>Tests two validation updates for equality.</summary>
        public static bool operator ==(
            DataGridGeneratedValidationChange<TKey> left,
            DataGridGeneratedValidationChange<TKey> right) => left.Equals(right);

        /// <summary>Tests two validation updates for inequality.</summary>
        public static bool operator !=(
            DataGridGeneratedValidationChange<TKey> left,
            DataGridGeneratedValidationChange<TKey> right) => !left.Equals(right);
    }

    /// <summary>
    /// Projects generated edit results as keyed errors, <see cref="INotifyDataErrorInfo"/>, and an
    /// <see cref="IObservable{T}"/> stream that ReactiveUI and other reactive frameworks can consume.
    /// </summary>
    /// <typeparam name="TItem">The row item type.</typeparam>
    /// <typeparam name="TKey">The stable row key type.</typeparam>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedValidationProjection<TItem, TKey> :
        INotifyDataErrorInfo,
        IObservable<DataGridGeneratedValidationChange<TKey>>,
        IDisposable
    {
        private readonly object _gate = new();
        private readonly IDataGridItemKey<TItem, TKey> _keyAccessor;
        private readonly DataGridGeneratedEditController<TItem, TKey> _controller;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly Dictionary<ValidationKey, ValidationError> _errors;
        private readonly List<IObserver<DataGridGeneratedValidationChange<TKey>>> _observers = new();
        private readonly bool _ownsController;
        private bool _disposed;

        /// <summary>Initializes a generated validation projection.</summary>
        public DataGridGeneratedValidationProjection(
            IDataGridItemKey<TItem, TKey> keyAccessor,
            DataGridGeneratedEditController<TItem, TKey> controller,
            IEqualityComparer<TKey> keyComparer = null,
            bool ownsController = false)
        {
            _keyAccessor = keyAccessor ?? throw new ArgumentNullException(nameof(keyAccessor));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _errors = new Dictionary<ValidationKey, ValidationError>(new ValidationKeyComparer(_keyComparer));
            _ownsController = ownsController;
        }

        /// <inheritdoc />
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        /// <summary>Gets the wrapped generated edit controller.</summary>
        public DataGridGeneratedEditController<TItem, TKey> Controller => _controller;

        /// <inheritdoc />
        public bool HasErrors
        {
            get
            {
                lock (_gate)
                {
                    return _errors.Count != 0;
                }
            }
        }

        /// <summary>Parses, validates, applies, and projects a generated text edit.</summary>
        public DataGridGeneratedEditResult TrySetText(
            TItem item,
            string columnKey,
            ReadOnlySpan<char> text,
            IFormatProvider formatProvider = null)
        {
            ThrowIfDisposed();
            DataGridGeneratedEditResult result = _controller.TrySetText(
                item,
                columnKey,
                text,
                formatProvider ?? CultureInfo.CurrentCulture);
            Publish(item, columnKey, result);
            return result;
        }

        /// <summary>Validates, applies, and projects a generated typed or boxed edit.</summary>
        public DataGridGeneratedEditResult TrySetValue(TItem item, string columnKey, object value)
        {
            ThrowIfDisposed();
            DataGridGeneratedEditResult result = _controller.TrySetValue(item, columnKey, value);
            Publish(item, columnKey, result);
            return result;
        }

        /// <summary>Runs revisioned asynchronous validation and projects only the terminal current result.</summary>
        public async ValueTask<DataGridGeneratedEditResult> TrySetValueAsync(
            TItem item,
            string columnKey,
            object value,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            DataGridGeneratedEditResult result = await _controller.TrySetValueAsync(
                item,
                columnKey,
                value,
                cancellationToken).ConfigureAwait(false);
            if (result.Status is not DataGridGeneratedEditStatus.Superseded and not DataGridGeneratedEditStatus.Cancelled)
            {
                Publish(item, columnKey, result);
            }
            return result;
        }

        /// <summary>Gets the current error for one stable row and column, or <see langword="null"/>.</summary>
        public string GetError(TKey itemKey, string columnKey)
        {
            if (columnKey == null)
            {
                throw new ArgumentNullException(nameof(columnKey));
            }
            lock (_gate)
            {
                return _errors.TryGetValue(new ValidationKey(itemKey, columnKey), out ValidationError error)
                    ? error.Message
                    : null;
            }
        }

        /// <summary>Clears the projected error for one row and column.</summary>
        public bool ClearError(TKey itemKey, string columnKey)
        {
            ThrowIfDisposed();
            if (columnKey == null)
            {
                throw new ArgumentNullException(nameof(columnKey));
            }
            string propertyName;
            lock (_gate)
            {
                var key = new ValidationKey(itemKey, columnKey);
                if (!_errors.TryGetValue(key, out ValidationError error))
                {
                    return false;
                }
                propertyName = error.PropertyName;
                _errors.Remove(key);
            }
            RaiseErrorsChanged(propertyName);
            PublishObservers(new DataGridGeneratedValidationChange<TKey>(
                itemKey,
                columnKey,
                propertyName,
                new DataGridGeneratedEditResult(DataGridGeneratedEditStatus.Applied),
                hasError: false));
            return true;
        }

        /// <summary>Clears all projected validation errors.</summary>
        public void ClearErrors()
        {
            ThrowIfDisposed();
            KeyValuePair<ValidationKey, ValidationError>[] cleared;
            string[] propertyNames;
            lock (_gate)
            {
                if (_errors.Count == 0)
                {
                    return;
                }
                var unique = new HashSet<string>(StringComparer.Ordinal);
                foreach (ValidationError error in _errors.Values)
                {
                    unique.Add(error.PropertyName);
                }
                cleared = new KeyValuePair<ValidationKey, ValidationError>[_errors.Count];
                int clearedIndex = 0;
                foreach (KeyValuePair<ValidationKey, ValidationError> pair in _errors)
                {
                    cleared[clearedIndex++] = pair;
                }
                propertyNames = new string[unique.Count];
                unique.CopyTo(propertyNames);
                _errors.Clear();
            }
            for (int index = 0; index < propertyNames.Length; index++)
            {
                RaiseErrorsChanged(propertyNames[index]);
            }
            for (int index = 0; index < cleared.Length; index++)
            {
                KeyValuePair<ValidationKey, ValidationError> pair = cleared[index];
                PublishObservers(new DataGridGeneratedValidationChange<TKey>(
                    pair.Key.ItemKey,
                    pair.Key.ColumnKey,
                    pair.Value.PropertyName,
                    new DataGridGeneratedEditResult(DataGridGeneratedEditStatus.Applied),
                    hasError: false));
            }
        }

        /// <inheritdoc />
        public IEnumerable GetErrors(string propertyName)
        {
            lock (_gate)
            {
                if (_errors.Count == 0)
                {
                    return Array.Empty<string>();
                }
                var messages = new List<string>();
                foreach (ValidationError error in _errors.Values)
                {
                    if (string.IsNullOrEmpty(propertyName) ||
                        string.Equals(error.PropertyName, propertyName, StringComparison.Ordinal))
                    {
                        messages.Add(error.Message);
                    }
                }
                return messages.Count == 0 ? Array.Empty<string>() : messages.ToArray();
            }
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<DataGridGeneratedValidationChange<TKey>> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }
            lock (_gate)
            {
                if (_disposed)
                {
                    observer.OnCompleted();
                    return EmptySubscription.Instance;
                }
                _observers.Add(observer);
                return new Subscription(this, observer);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            IObserver<DataGridGeneratedValidationChange<TKey>>[] observers;
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                _errors.Clear();
                observers = _observers.ToArray();
                _observers.Clear();
            }
            for (int index = 0; index < observers.Length; index++)
            {
                observers[index].OnCompleted();
            }
            if (_ownsController)
            {
                _controller.Dispose();
            }
        }

        private void Publish(TItem item, string columnKey, DataGridGeneratedEditResult result)
        {
            TKey itemKey = _keyAccessor.GetKey(item);
            IDataGridGeneratedEditField<TItem> field = GetField(columnKey);
            bool resultHasError = !result.IsApplied && !string.IsNullOrEmpty(result.Error);
            bool hasError;
            bool changed = false;
            lock (_gate)
            {
                var key = new ValidationKey(itemKey, columnKey);
                if (resultHasError)
                {
                    var next = new ValidationError(field.PropertyName, result.Error);
                    changed = !_errors.TryGetValue(key, out ValidationError current) || !current.Equals(next);
                    _errors[key] = next;
                }
                else if (result.IsApplied)
                {
                    changed = _errors.Remove(key);
                }
                hasError = _errors.ContainsKey(key);
            }
            if (changed)
            {
                RaiseErrorsChanged(field.PropertyName);
            }
            PublishObservers(new DataGridGeneratedValidationChange<TKey>(
                itemKey,
                columnKey,
                field.PropertyName,
                result,
                hasError));
        }

        private IDataGridGeneratedEditField<TItem> GetField(string columnKey) =>
            _controller.Fields.TryGetValue(columnKey, out IDataGridGeneratedEditField<TItem> field)
                ? field
                : throw new KeyNotFoundException("Generated edit field '" + columnKey + "' was not found.");

        private void RaiseErrorsChanged(string propertyName) =>
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

        private void PublishObservers(DataGridGeneratedValidationChange<TKey> change)
        {
            IObserver<DataGridGeneratedValidationChange<TKey>>[] observers;
            lock (_gate)
            {
                if (_disposed || _observers.Count == 0)
                {
                    return;
                }
                observers = _observers.ToArray();
            }
            for (int index = 0; index < observers.Length; index++)
            {
                observers[index].OnNext(change);
            }
        }

        private void Unsubscribe(IObserver<DataGridGeneratedValidationChange<TKey>> observer)
        {
            lock (_gate)
            {
                _observers.Remove(observer);
            }
        }

        private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

        private readonly struct ValidationKey
        {
            public ValidationKey(TKey itemKey, string columnKey)
            {
                ItemKey = itemKey;
                ColumnKey = columnKey;
            }

            public TKey ItemKey { get; }
            public string ColumnKey { get; }
        }

        private readonly struct ValidationError : IEquatable<ValidationError>
        {
            public ValidationError(string propertyName, string message)
            {
                PropertyName = propertyName;
                Message = message;
            }

            public string PropertyName { get; }
            public string Message { get; }

            public bool Equals(ValidationError other) =>
                string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal) &&
                string.Equals(Message, other.Message, StringComparison.Ordinal);
        }

        private sealed class ValidationKeyComparer : IEqualityComparer<ValidationKey>
        {
            private readonly IEqualityComparer<TKey> _keyComparer;

            public ValidationKeyComparer(IEqualityComparer<TKey> keyComparer) => _keyComparer = keyComparer;

            public bool Equals(ValidationKey left, ValidationKey right) =>
                _keyComparer.Equals(left.ItemKey, right.ItemKey) &&
                string.Equals(left.ColumnKey, right.ColumnKey, StringComparison.Ordinal);

            public int GetHashCode(ValidationKey value) =>
                HashCode.Combine(_keyComparer.GetHashCode(value.ItemKey), StringComparer.Ordinal.GetHashCode(value.ColumnKey));
        }

        private sealed class Subscription : IDisposable
        {
            private DataGridGeneratedValidationProjection<TItem, TKey> _owner;
            private IObserver<DataGridGeneratedValidationChange<TKey>> _observer;

            public Subscription(
                DataGridGeneratedValidationProjection<TItem, TKey> owner,
                IObserver<DataGridGeneratedValidationChange<TKey>> observer)
            {
                _owner = owner;
                _observer = observer;
            }

            public void Dispose()
            {
                DataGridGeneratedValidationProjection<TItem, TKey> owner = Interlocked.Exchange(ref _owner, null);
                IObserver<DataGridGeneratedValidationChange<TKey>> observer = Interlocked.Exchange(ref _observer, null);
                if (owner != null && observer != null)
                {
                    owner.Unsubscribe(observer);
                }
            }
        }

        private sealed class EmptySubscription : IDisposable
        {
            public static EmptySubscription Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
