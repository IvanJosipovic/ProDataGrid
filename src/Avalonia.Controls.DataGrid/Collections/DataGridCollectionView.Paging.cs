// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

#nullable disable

using Avalonia.Controls.Utils;
using Avalonia.Utilities;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System;

namespace Avalonia.Collections
{
    sealed partial class DataGridCollectionView
    {
        internal bool IsPageProjectionChanging { get; private set; }

        /// <summary>
        /// Moves to the first page.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        //TODO Paging
        public bool MoveToFirstPage()
        {
            return MoveToPage(0);
        }

        /// <summary>
        /// Moves to the last page.
        /// The move is only attempted when TotalItemCount is known.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        //TODO Paging
        public bool MoveToLastPage()
        {
            if (TotalItemCount != -1 && PageSize > 0)
            {
                return MoveToPage(PageCount - 1);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Moves to the page after the current page we are on.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        //TODO Paging
        public bool MoveToNextPage()
        {
            return MoveToPage(_pageIndex + 1);
        }

        /// <summary>
        /// Requests a page move to page <paramref name="pageIndex"/>.
        /// </summary>
        /// <param name="pageIndex">Index of the target page</param>
        /// <returns>Whether or not the move was successfully initiated.</returns>
        //TODO Paging
        public bool MoveToPage(int pageIndex)
        {
            // Boundary checks for negative pageIndex
            if (pageIndex < -1)
            {
                return false;
            }

            // if the Refresh is deferred, cache the requested PageIndex so that we
            // can move to the desired page when EndDefer is called.
            if (IsRefreshDeferred)
            {
                // set cached value and flag so that we move to the page on EndDefer
                _cachedPageIndex = pageIndex;
                SetFlag(CollectionViewFlags.IsMoveToPageDeferred, true);
                return false;
            }

            // check for invalid pageIndex
            if (pageIndex == -1 && PageSize > 0)
            {
                return false;
            }

            // Check if the target page is out of bound, or equal to the current page
            if (pageIndex >= PageCount || _pageIndex == pageIndex)
            {
                return false;
            }

            // Check with the ICollectionView.CurrentChanging listeners if it's OK to move
            // on to another page
            if (!OkToChangeCurrent())
            {
                return false;
            }

            if (RaisePageChanging(pageIndex) && pageIndex != -1)
            {
                // Page move was cancelled. Abort the move, but only if the target index isn't -1.
                return false;
            }

            // Check if there is a current edited or new item so changes can be committed first.
            if (CurrentAddItem != null || CurrentEditItem != null)
            {
                // Remember current currency values for upcoming OnPropertyChanged notifications
                object oldCurrentItem = CurrentItem;
                int oldCurrentPosition = CurrentPosition;
                bool oldIsCurrentAfterLast = IsCurrentAfterLast;
                bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

                // Currently CommitNew()/CommitEdit()/CancelNew()/CancelEdit() can't handle committing or
                // cancelling an item that is no longer on the current page. That's acceptable and means that
                // the potential _newItem or _editItem needs to be committed before this page move.
                // The reason why we temporarily reset currency here is to give a chance to the bound
                // controls to commit or cancel their potential edits/addition. The DataForm calls ForceEndEdit()
                // for example as a result of changing currency.
                SetCurrentToPosition(-1);
                RaiseCurrencyChanges(true /*fireChangedEvent*/, oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);

                // If the bound controls did not successfully end their potential item editing/addition, the
                // page move needs to be aborted.
                if (CurrentAddItem != null || CurrentEditItem != null)
                {
                    // Since PageChanging was raised and not cancelled, a PageChanged notification needs to be raised
                    // even though the PageIndex actually did not change.
                    RaisePageChanged();

                    // Restore original currency
                    Debug.Assert(CurrentItem == null, "Unexpected CurrentItem != null");
                    Debug.Assert(CurrentPosition == -1, "Unexpected CurrentPosition != -1");
                    Debug.Assert(IsCurrentBeforeFirst, "Unexpected IsCurrentBeforeFirst == false");
                    Debug.Assert(!IsCurrentAfterLast, "Unexpected IsCurrentAfterLast == true");

                    SetCurrentToPosition(oldCurrentPosition);
                    RaiseCurrencyChanges(false /*fireChangedEvent*/, null /*oldCurrentItem*/, -1 /*oldCurrentPosition*/,
                    true /*oldIsCurrentBeforeFirst*/, false /*oldIsCurrentAfterLast*/);

                    return false;
                }

                // Finally raise a CurrentChanging notification for the upcoming currency change
                // that will occur in CompletePageMove(pageIndex).
                OnCurrentChanging();
            }

            IsPageChanging = true;
            CompletePageMove(pageIndex);

            return true;
        }

        /// <summary>
        /// Moves to the page before the current page we are on.
        /// </summary>
        /// <returns>Whether or not the move was successful.</returns>
        //TODO Paging
        public bool MoveToPreviousPage()
        {
            return MoveToPage(_pageIndex - 1);
        }

        /// <summary>
        /// Called to complete the page move operation to set the
        /// current page index.
        /// </summary>
        /// <param name="pageIndex">Final page index</param>
        //TODO Paging
        private void CompletePageMove(int pageIndex)
        {
            Debug.Assert(_pageIndex != pageIndex, "Unexpected _pageIndex == pageIndex");

            // to see whether or not to fire an OnPropertyChanged
            int oldCount = Count;
            object oldCurrentItem = CurrentItem;
            int oldCurrentPosition = CurrentPosition;
            bool oldIsCurrentAfterLast = IsCurrentAfterLast;
            bool oldIsCurrentBeforeFirst = IsCurrentBeforeFirst;

            _pageIndex = pageIndex;

            // update the groups
            if (IsGrouping && PageSize > 0)
            {
                PrepareGroupsForCurrentPage();
            }

            // update currency
            if (Count >= 1)
            {
                SetCurrent(GetItemAt(0), 0);
            }
            else
            {
                SetCurrent(null, -1);
            }

            IsPageChanging = false;
            OnPropertyChanged(nameof(PageIndex));
            RaisePageChanged();

            // if the count has changed
            if (Count != oldCount)
            {
                OnPropertyChanged(nameof(Count));
            }

            IsPageProjectionChanging = true;
            try
            {
                OnCollectionChanged(
                    new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset));
            }
            finally
            {
                IsPageProjectionChanging = false;
            }

            // Always raise CurrentChanged since the calling method MoveToPage(pageIndex) raised CurrentChanging.
            RaiseCurrencyChanges(true /*fireChangedEvent*/, oldCurrentItem, oldCurrentPosition, oldIsCurrentBeforeFirst, oldIsCurrentAfterLast);
        }

        /// <summary>
        /// Raises the PageChanged event
        /// </summary>
        private void RaisePageChanged()
        {
            PageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the PageChanging event
        /// </summary>
        /// <param name="newPageIndex">Index of the requested page</param>
        /// <returns>True if the event is cancelled (e.Cancel was set to True), False otherwise</returns>
        private bool RaisePageChanging(int newPageIndex)
        {
            EventHandler<PageChangingEventArgs> handler = PageChanging;
            if (handler != null)
            {
                PageChangingEventArgs pageChangingEventArgs = new PageChangingEventArgs(newPageIndex);
                handler(this, pageChangingEventArgs);
                return pageChangingEventArgs.Cancel;
            }

            return false;
        }

    }
}
