// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using Avalonia.Collections;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Avalonia.Controls
{
    /// <summary>
    /// Validation handling
    /// </summary>
#if !DATAGRID_INTERNAL
public
#else
internal
#endif
    partial class DataGrid
    {

        //TODO Validation UI
        private void ResetValidationStatus(DataGridCell editingCell = null)
        {
            // Clear the invalid status of the Cell, Row and DataGrid
            if (EditingRow != null)
            {
                if (EditingRow.Index != -1)
                {
                    if (editingCell != null)
                    {
                        ClearCellValidation(editingCell);
                    }
                    else
                    {
                        foreach (DataGridCell cell in EditingRow.Cells)
                        {
                            ClearCellValidation(cell);
                        }
                    }
                    UpdateRowValidationStateFromCells(EditingRow);
                }
            }
            UpdateGridValidationState();

            _validationSubscription?.Dispose();
            _validationSubscription = null;
        }

        private void ClearCellValidation(DataGridCell cell)
        {
            _notifyDataErrorInfoCellErrors.Remove(cell);

            if (!cell.IsValid || cell.ValidationSeverity != DataGridValidationSeverity.None)
            {
                cell.IsValid = true;
                cell.ValidationSeverity = DataGridValidationSeverity.None;
                cell.UpdatePseudoClasses();
            }

            DataValidationErrors.ClearErrors(cell);
        }

        private void RestoreRowValidationState(
            DataGridRow row,
            object item,
            bool clearIfNoIndei = true,
            string propertyName = null,
            int excludedColumnIndex = -1)
        {
            if (row is null)
            {
                return;
            }

            if (item is null || ReferenceEquals(item, DataGridCollectionView.NewItemPlaceholder))
            {
                ClearRowValidation(row);
                return;
            }

            if (item is not INotifyDataErrorInfo notifyDataErrorInfo)
            {
                if (clearIfNoIndei)
                {
                    ClearRowValidation(row);
                }
                return;
            }

            foreach (var column in ColumnsItemsInternal)
            {
                if (column.Index < 0 || column.Index >= row.Cells.Count)
                {
                    continue;
                }

                if (column.Index == excludedColumnIndex)
                {
                    continue;
                }

                var cell = row.Cells[column.Index];
                var bindingPath = GetColumnBindingPath(column);

                if (string.IsNullOrWhiteSpace(bindingPath))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(propertyName) &&
                    !string.Equals(bindingPath, propertyName, StringComparison.Ordinal))
                {
                    continue;
                }

                var errors = notifyDataErrorInfo.GetErrors(bindingPath);
                if (errors is null)
                {
                    ClearNotifyDataErrorInfoValidation(cell);
                    continue;
                }

                var exceptions = CreateValidationExceptions(errors);
                if (exceptions.Count == 0)
                {
                    ClearNotifyDataErrorInfoValidation(cell);
                    continue;
                }

                var preservedErrors = GetErrorsWithoutOwnedNotifyDataErrorInfoErrors(cell);
                var severity = MaxSeverity(
                    ValidationUtil.GetValidationSeverity(exceptions),
                    GetDisplayedValidationSeverity(preservedErrors));
                cell.IsValid = severity != DataGridValidationSeverity.Error;
                cell.ValidationSeverity = severity;
                cell.UpdatePseudoClasses();

                var errorException = exceptions.Count == 1
                    ? exceptions[0]
                    : new AggregateException(exceptions);
                preservedErrors.Add(errorException);
                DataValidationErrors.SetErrors(cell, preservedErrors);
                _notifyDataErrorInfoCellErrors[cell] = new object[] { errorException };
            }

            UpdateRowValidationStateFromCells(row);
            UpdateGridValidationState();
        }

        private void ClearNotifyDataErrorInfoValidation(DataGridCell cell)
        {
            if (!_notifyDataErrorInfoCellErrors.Remove(cell, out object[] expectedErrors))
            {
                return;
            }

            List<object> preservedErrors = GetErrorsWithoutOwnedErrors(cell, expectedErrors);
            if (preservedErrors.Count == 0)
            {
                ClearCellValidation(cell);
                return;
            }

            DataValidationErrors.SetErrors(cell, preservedErrors);
            DataGridValidationSeverity severity = GetDisplayedValidationSeverity(preservedErrors);
            cell.IsValid = severity != DataGridValidationSeverity.Error;
            cell.ValidationSeverity = severity;
            cell.UpdatePseudoClasses();
        }

        private List<object> GetErrorsWithoutOwnedNotifyDataErrorInfoErrors(DataGridCell cell)
        {
            return _notifyDataErrorInfoCellErrors.TryGetValue(cell, out object[] ownedErrors)
                ? GetErrorsWithoutOwnedErrors(cell, ownedErrors)
                : GetDisplayedErrors(cell);
        }

        private static List<object> GetErrorsWithoutOwnedErrors(DataGridCell cell, object[] ownedErrors)
        {
            List<object> errors = GetDisplayedErrors(cell);
            foreach (object ownedError in ownedErrors)
            {
                for (int index = 0; index < errors.Count; index++)
                {
                    if (ReferenceEquals(errors[index], ownedError))
                    {
                        errors.RemoveAt(index);
                        break;
                    }
                }
            }

            return errors;
        }

        private static List<object> GetDisplayedErrors(DataGridCell cell)
        {
            var errors = new List<object>();
            IEnumerable<object> displayedErrors = DataValidationErrors.GetErrors(cell);
            if (displayedErrors is null)
            {
                return errors;
            }

            foreach (object error in displayedErrors)
            {
                if (error is not null)
                {
                    errors.Add(error);
                }
            }

            return errors;
        }

        private static DataGridValidationSeverity GetDisplayedValidationSeverity(List<object> errors)
        {
            DataGridValidationSeverity severity = DataGridValidationSeverity.None;
            foreach (object error in errors)
            {
                severity = MaxSeverity(severity, GetDisplayedValidationSeverity(error));
            }

            return severity;
        }

        private static DataGridValidationSeverity GetDisplayedValidationSeverity(object error)
        {
            if (error is AggregateException aggregateException)
            {
                DataGridValidationSeverity severity = DataGridValidationSeverity.None;
                foreach (Exception innerException in aggregateException.InnerExceptions)
                {
                    severity = MaxSeverity(severity, GetDisplayedValidationSeverity(innerException));
                }

                return severity;
            }

            return ValidationUtil.GetValidationSeverity(
                error as Exception ?? new DataValidationException(error));
        }

        private static DataGridValidationSeverity MaxSeverity(
            DataGridValidationSeverity first,
            DataGridValidationSeverity second)
        {
            return first >= second ? first : second;
        }

        private void ClearRowValidation(DataGridRow row)
        {
            if (row is null)
            {
                return;
            }

            row.IsValid = true;
            row.ValidationSeverity = DataGridValidationSeverity.None;

            foreach (DataGridCell cell in row.Cells)
            {
                _notifyDataErrorInfoCellErrors.Remove(cell);
                ClearCellValidation(cell);
            }

            row.ApplyState();
            UpdateGridValidationState();
        }

        private static bool RowHasError(DataGridRow row)
        {
            foreach (DataGridCell cell in row.Cells)
            {
                if (cell.ValidationSeverity == DataGridValidationSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateRowValidationStateFromCells(DataGridRow row)
        {
            if (row is null)
            {
                return;
            }

            var hasError = RowHasError(row);
            row.IsValid = !hasError;
            row.ValidationSeverity = hasError ? DataGridValidationSeverity.Error : DataGridValidationSeverity.None;
            row.ApplyState();
        }

        private void UpdateGridValidationState()
        {
            var hasError = false;

            if (DisplayData != null)
            {
                for (int slot = DisplayData.FirstScrollingSlot;
                    slot > -1 && slot <= DisplayData.LastScrollingSlot;
                    slot++)
                {
                    if (DisplayData.GetDisplayedElement(slot) is DataGridRow row &&
                        (!row.IsValid || row.ValidationSeverity == DataGridValidationSeverity.Error))
                    {
                        hasError = true;
                        break;
                    }
                }
            }

            if (!hasError && EditingRow != null && !EditingRow.IsValid)
            {
                hasError = true;
            }

            if (!hasError && CollectionViewHasError())
            {
                hasError = true;
            }

            IsValid = !hasError;
        }

        private bool CollectionViewHasError()
        {
            EnsureCollectionValidationState();
            return _collectionValidationItemsWithError.Count > 0;
        }

        private bool ItemHasError(INotifyDataErrorInfo notifyDataErrorInfo)
        {
            foreach (var column in ColumnsItemsInternal)
            {
                var bindingPath = GetColumnBindingPath(column);
                if (string.IsNullOrWhiteSpace(bindingPath))
                {
                    continue;
                }

                var errors = notifyDataErrorInfo.GetErrors(bindingPath);
                if (errors is null)
                {
                    continue;
                }

                var exceptions = CreateValidationExceptions(errors);
                if (exceptions.Count == 0)
                {
                    continue;
                }

                if (ValidationUtil.GetValidationSeverity(exceptions) == DataGridValidationSeverity.Error)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDataSourceChangedForValidation()
        {
            InvalidateCollectionValidationState(clearTracking: true);
            if (!IsAttachedToVisualTree)
            {
                return;
            }

            UpdateGridValidationState();
        }

        internal void OnCollectionChangedForValidation(NotifyCollectionChangedEventArgs e)
        {
            if (!_collectionValidationStateInitialized)
            {
                return;
            }

            if (e == null)
            {
                InvalidateCollectionValidationState(clearTracking: true);
                UpdateGridValidationState();
                return;
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    TrackCollectionValidationItems(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UntrackCollectionValidationItems(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    UntrackCollectionValidationItems(e.OldItems);
                    TrackCollectionValidationItems(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Move:
                    return;
                case NotifyCollectionChangedAction.Reset:
                    InvalidateCollectionValidationState(clearTracking: true);
                    break;
            }

            UpdateGridValidationState();
        }

        private void OnColumnsChangedForValidation()
        {
            if (!_collectionValidationStateInitialized)
            {
                return;
            }

            InvalidateCollectionValidationState(clearTracking: false);
            UpdateGridValidationState();
        }

        private void OnColumnBindingChangedForValidation()
        {
            OnColumnsChangedForValidation();
        }

        private void EnsureCollectionValidationState()
        {
            if (!_collectionValidationStateInitialized)
            {
                BuildCollectionValidationState();
                return;
            }

            if (_collectionValidationStateInvalidated)
            {
                ReevaluateTrackedCollectionValidationItems();
            }
        }

        private void BuildCollectionValidationState()
        {
            DetachCollectionValidationTracking();

            var collectionView = DataConnection?.CollectionView;
            if (collectionView == null)
            {
                _collectionValidationStateInitialized = true;
                _collectionValidationStateInvalidated = false;
                return;
            }

            foreach (var item in collectionView)
            {
                if (!TryGetCollectionValidationItem(item, out var notifyDataErrorInfo))
                {
                    continue;
                }

                TrackCollectionValidationItem(notifyDataErrorInfo);
                UpdateTrackedCollectionValidationItemState(notifyDataErrorInfo);
            }

            _collectionValidationStateInitialized = true;
            _collectionValidationStateInvalidated = false;
        }

        private void ReevaluateTrackedCollectionValidationItems()
        {
            _collectionValidationItemsWithError.Clear();
            foreach (var notifyDataErrorInfo in _collectionValidationTrackedItems)
            {
                UpdateTrackedCollectionValidationItemState(notifyDataErrorInfo);
            }

            _collectionValidationStateInvalidated = false;
        }

        private void InvalidateCollectionValidationState(bool clearTracking)
        {
            _collectionValidationStateInvalidated = true;
            if (!clearTracking)
            {
                return;
            }

            DetachCollectionValidationTracking();
            _collectionValidationStateInitialized = false;
        }

        private void TrackCollectionValidationItems(IList items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (!TryGetCollectionValidationItem(item, out var notifyDataErrorInfo))
                {
                    continue;
                }

                TrackCollectionValidationItem(notifyDataErrorInfo);
                UpdateTrackedCollectionValidationItemState(notifyDataErrorInfo);
            }
        }

        private void UntrackCollectionValidationItems(IList items)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item is INotifyDataErrorInfo notifyDataErrorInfo)
                {
                    UntrackCollectionValidationItem(notifyDataErrorInfo);
                }
            }
        }

        private static bool TryGetCollectionValidationItem(object item, out INotifyDataErrorInfo notifyDataErrorInfo)
        {
            notifyDataErrorInfo = null;
            if (item == null || ReferenceEquals(item, DataGridCollectionView.NewItemPlaceholder))
            {
                return false;
            }

            if (item is not INotifyDataErrorInfo indei)
            {
                return false;
            }

            notifyDataErrorInfo = indei;
            return true;
        }

        private void TrackCollectionValidationItem(INotifyDataErrorInfo notifyDataErrorInfo)
        {
            if (!_collectionValidationTrackedItems.Add(notifyDataErrorInfo))
            {
                return;
            }

            WeakEventHandlerManager.Subscribe<INotifyDataErrorInfo, DataErrorsChangedEventArgs, DataGrid>(
                notifyDataErrorInfo,
                nameof(INotifyDataErrorInfo.ErrorsChanged),
                CollectionValidationItem_ErrorsChanged);
        }

        private void UntrackCollectionValidationItem(INotifyDataErrorInfo notifyDataErrorInfo)
        {
            if (!_collectionValidationTrackedItems.Remove(notifyDataErrorInfo))
            {
                return;
            }

            WeakEventHandlerManager.Unsubscribe<DataErrorsChangedEventArgs, DataGrid>(
                notifyDataErrorInfo,
                nameof(INotifyDataErrorInfo.ErrorsChanged),
                CollectionValidationItem_ErrorsChanged);
            _collectionValidationItemsWithError.Remove(notifyDataErrorInfo);
        }

        private void UpdateTrackedCollectionValidationItemState(INotifyDataErrorInfo notifyDataErrorInfo)
        {
            if (!notifyDataErrorInfo.HasErrors || !ItemHasError(notifyDataErrorInfo))
            {
                _collectionValidationItemsWithError.Remove(notifyDataErrorInfo);
                return;
            }

            _collectionValidationItemsWithError.Add(notifyDataErrorInfo);
        }

        private void CollectionValidationItem_ErrorsChanged(object sender, DataErrorsChangedEventArgs e)
        {
            if (sender is not INotifyDataErrorInfo notifyDataErrorInfo)
            {
                return;
            }

            if (!_collectionValidationTrackedItems.Contains(notifyDataErrorInfo))
            {
                return;
            }

            UpdateTrackedCollectionValidationItemState(notifyDataErrorInfo);
            RefreshRealizedRowValidationState(notifyDataErrorInfo, e.PropertyName);
            UpdateGridValidationState();
        }

        private void RefreshRealizedRowValidationState(
            INotifyDataErrorInfo notifyDataErrorInfo,
            string propertyName)
        {
            if (DisplayData == null)
            {
                return;
            }

            for (int slot = DisplayData.FirstScrollingSlot;
                slot > -1 && slot <= DisplayData.LastScrollingSlot;
                slot++)
            {
                if (DisplayData.GetDisplayedElement(slot) is not DataGridRow row ||
                    !ReferenceEquals(row.DataContext, notifyDataErrorInfo))
                {
                    continue;
                }

                int excludedColumnIndex = ReferenceEquals(row, EditingRow)
                    ? _editingColumnIndex
                    : -1;
                RestoreRowValidationState(
                    row,
                    notifyDataErrorInfo,
                    propertyName: propertyName,
                    excludedColumnIndex: excludedColumnIndex);
            }
        }

        private void DetachCollectionValidationTracking()
        {
            foreach (var notifyDataErrorInfo in _collectionValidationTrackedItems)
            {
                WeakEventHandlerManager.Unsubscribe<DataErrorsChangedEventArgs, DataGrid>(
                    notifyDataErrorInfo,
                    nameof(INotifyDataErrorInfo.ErrorsChanged),
                    CollectionValidationItem_ErrorsChanged);
            }

            _collectionValidationTrackedItems.Clear();
            _collectionValidationItemsWithError.Clear();
        }

        private static string GetBindingPath(BindingBase binding)
        {
            return BindingCloneHelper.GetPath(binding);
        }

        private static string GetColumnBindingPath(DataGridColumn column)
        {
            if (column is DataGridBoundColumn boundColumn)
            {
                return boundColumn.Binding is { } binding
                    ? GetBindingPath(binding)
                    : null;
            }

            if (column is DataGridComboBoxColumn comboBoxColumn)
            {
                return comboBoxColumn.EffectiveBinding is { } binding
                    ? GetBindingPath(binding)
                    : null;
            }

            return null;
        }

        private static List<Exception> CreateValidationExceptions(IEnumerable errors)
        {
            var exceptions = new List<Exception>();

            foreach (var error in errors)
            {
                if (error is null)
                {
                    continue;
                }

                if (error is Exception exception)
                {
                    exceptions.Add(exception);
                }
                else
                {
                    exceptions.Add(new DataValidationException(error));
                }
            }

            return exceptions;
        }

        private List<Exception> _bindingValidationErrors;

        private IDisposable _validationSubscription;

        private bool _isValid = true;
        private readonly HashSet<INotifyDataErrorInfo> _collectionValidationTrackedItems = new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<INotifyDataErrorInfo> _collectionValidationItemsWithError = new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<DataGridCell, object[]> _notifyDataErrorInfoCellErrors = new();
        private bool _collectionValidationStateInitialized;
        private bool _collectionValidationStateInvalidated = true;


        public bool IsValid
        {
            get { return _isValid; }
            internal set
            {
                SetAndRaise(IsValidProperty, ref _isValid, value);
                PseudoClassesHelper.Set(PseudoClasses, ":invalid", !value);
            }
        }

    }
}
