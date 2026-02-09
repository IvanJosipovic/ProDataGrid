// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Headless.XUnit;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.VisualTree;
using Xunit;
using System.Reflection;

namespace Avalonia.Controls.DataGridTests
{
    /// <summary>
    /// Tests for the ILogicalScrollable implementation on DataGridRowsPresenter.
    /// </summary>
    public class DataGridRowsPresenterTests
    {
        [AvaloniaFact]
        public void DataGridRowsPresenter_Implements_ILogicalScrollable()
        {
            // Arrange & Act
            var presenter = new DataGridRowsPresenter();

            // Assert
            Assert.IsAssignableFrom<ILogicalScrollable>(presenter);
        }

        [AvaloniaFact]
        public void IsLogicalScrollEnabled_Returns_False_By_Default()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            bool result = ((ILogicalScrollable)presenter).IsLogicalScrollEnabled;

            // Assert - without OwningGrid or with UseLogicalScrollable=false, returns false
            Assert.False(result);
        }

        [AvaloniaFact]
        public void CanVerticallyScroll_Default_Is_True()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            bool result = presenter.CanVerticallyScroll;

            // Assert
            Assert.True(result);
        }

        [AvaloniaFact]
        public void CanHorizontallyScroll_Default_Is_False()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            bool result = presenter.CanHorizontallyScroll;

            // Assert
            Assert.False(result);
        }

        [AvaloniaFact]
        public void CanVerticallyScroll_Can_Be_Set()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            presenter.CanVerticallyScroll = false;

            // Assert
            Assert.False(presenter.CanVerticallyScroll);
        }

        [AvaloniaFact]
        public void CanHorizontallyScroll_Can_Be_Set()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            presenter.CanHorizontallyScroll = true;

            // Assert
            Assert.True(presenter.CanHorizontallyScroll);
        }

        [AvaloniaFact]
        public void Extent_Defaults_To_Zero()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            var extent = presenter.Extent;

            // Assert
            Assert.Equal(new Size(0, 0), extent);
        }

        [AvaloniaFact]
        public void Viewport_Defaults_To_Zero()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            var viewport = presenter.Viewport;

            // Assert
            Assert.Equal(new Size(0, 0), viewport);
        }

        [AvaloniaFact]
        public void Offset_Defaults_To_Zero()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            var offset = presenter.Offset;

            // Assert
            Assert.Equal(new Vector(0, 0), offset);
        }

        [AvaloniaFact]
        public void ScrollSize_Returns_Reasonable_Defaults()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            var scrollSize = presenter.ScrollSize;

            // Assert - without OwningGrid, should use default row height of 22
            Assert.Equal(16, scrollSize.Width);
            Assert.Equal(22, scrollSize.Height);
        }

        [AvaloniaFact]
        public void PageScrollSize_Returns_Viewport()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            presenter.UpdateScrollInfo(new Size(500, 2000), new Size(300, 400));
            var pageScrollSize = presenter.PageScrollSize;

            // Assert
            Assert.Equal(presenter.Viewport, pageScrollSize);
        }

        [AvaloniaFact]
        public void UpdateScrollInfo_Updates_Extent_And_Viewport()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var newExtent = new Size(500, 2000);
            var newViewport = new Size(300, 400);

            // Act
            presenter.UpdateScrollInfo(newExtent, newViewport);

            // Assert
            Assert.Equal(newExtent, presenter.Extent);
            Assert.Equal(newViewport, presenter.Viewport);
        }

        [AvaloniaFact]
        public void UpdateScrollInfo_Raises_ScrollInvalidated()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            bool eventRaised = false;
            presenter.ScrollInvalidated += (s, e) => eventRaised = true;

            // Act
            presenter.UpdateScrollInfo(new Size(500, 2000), new Size(300, 400));

            // Assert
            Assert.True(eventRaised);
        }

        [AvaloniaFact]
        public void UpdateScrollInfo_Does_Not_Raise_ScrollInvalidated_When_Values_Same()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var extent = new Size(500, 2000);
            var viewport = new Size(300, 400);
            presenter.UpdateScrollInfo(extent, viewport);

            bool eventRaised = false;
            presenter.ScrollInvalidated += (s, e) => eventRaised = true;

            // Act - set same values again
            presenter.UpdateScrollInfo(extent, viewport);

            // Assert
            Assert.False(eventRaised);
        }

        [AvaloniaFact]
        public void SyncOffset_Updates_Offset_Without_Side_Effects()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(500, 2000), new Size(300, 400));

            // Act
            presenter.SyncOffset(50.0, 100.0);

            // Assert
            Assert.Equal(new Vector(50.0, 100.0), presenter.Offset);
        }

        [AvaloniaFact]
        public void Offset_Setter_Coerces_To_Valid_Range()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(500, 2000), new Size(300, 400));

            // Act - try to set offset beyond extent
            presenter.Offset = new Vector(1000, 5000);

            // Assert - should be coerced to max scrollable extent
            Assert.Equal(200, presenter.Offset.X);  // 500 - 300
            Assert.Equal(1600, presenter.Offset.Y); // 2000 - 400
        }

        [AvaloniaFact]
        public void Offset_Setter_Coerces_Negative_Values_To_Zero()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(500, 2000), new Size(300, 400));

            // Act
            presenter.Offset = new Vector(-100, -200);

            // Assert
            Assert.Equal(new Vector(0, 0), presenter.Offset);
        }

        [AvaloniaFact]
        public void UpdateScrollInfo_Coerces_Existing_Offset()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(500, 2000), new Size(300, 400));
            presenter.SyncOffset(150, 1500);

            // Act - shrink extent so offset is out of range
            presenter.UpdateScrollInfo(new Size(400, 1000), new Size(300, 400));

            // Assert - offset should be coerced to max
            Assert.Equal(100, presenter.Offset.X);  // 400 - 300
            Assert.Equal(600, presenter.Offset.Y);  // 1000 - 400
        }

        [AvaloniaFact]
        public void BringIntoView_Returns_False_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            var result = presenter.BringIntoView(new Button(), new Rect(0, 0, 100, 20));

            // Assert
            Assert.False(result);
        }

        [AvaloniaFact]
        public void GetControlInDirection_Returns_Null_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            var result = presenter.GetControlInDirection(NavigationDirection.Down, null);

            // Assert
            Assert.Null(result);
        }

        #region Horizontal Scrolling Tests

        [AvaloniaFact]
        public void Offset_Horizontal_Changes_Are_Applied()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(1000, 500), new Size(400, 300));

            // Act
            presenter.Offset = new Vector(200, 0);

            // Assert
            Assert.Equal(200, presenter.Offset.X);
        }

        [AvaloniaFact]
        public void Offset_Horizontal_Is_Coerced_To_Max_Scrollable()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(500, 500), new Size(300, 300));

            // Act - try to scroll beyond max
            presenter.Offset = new Vector(500, 0);

            // Assert - should be coerced to max (extent.width - viewport.width)
            Assert.Equal(200, presenter.Offset.X);
        }

        [AvaloniaFact]
        public void PageScrollSize_Reflects_Viewport_For_Horizontal()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(1000, 2000), new Size(400, 500));

            // Act
            var pageScrollSize = presenter.PageScrollSize;

            // Assert - without OwningGrid, frozen width is 0
            Assert.Equal(400, pageScrollSize.Width);
        }

        [AvaloniaFact]
        public void ScrollSize_Has_Reasonable_Horizontal_Default()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            var scrollSize = presenter.ScrollSize;

            // Assert - without OwningGrid, uses default column width of 16
            Assert.True(scrollSize.Width > 0);
        }

        [AvaloniaFact]
        public void ScrollSize_With_Wide_First_Column_Uses_Default_Horizontal_Step()
        {
            var items = Enumerable.Range(0, 20)
                .Select(i => new { Name = $"Item {i}", Value = i })
                .ToList();

            var root = new Window
            {
                Width = 320,
                Height = 240,
            };

            root.SetThemeStyles(DataGridTheme.FluentV2);

            var grid = new DataGrid
            {
                ItemsSource = items,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                UseLogicalScrollable = true,
            };

            grid.ColumnsInternal.Add(new DataGridTextColumn
            {
                Header = "Wide",
                Binding = new Binding("Name"),
                Width = new DataGridLength(400),
            });
            grid.ColumnsInternal.Add(new DataGridTextColumn
            {
                Header = "Value",
                Binding = new Binding("Value"),
                Width = new DataGridLength(120),
            });

            root.Content = grid;
            root.Show();
            root.UpdateLayout();

            var presenter = grid.GetVisualDescendants()
                .OfType<DataGridRowsPresenter>()
                .First();

            var scrollSize = presenter.ScrollSize;

            Assert.Equal(16, scrollSize.Width);
            Assert.True(scrollSize.Height > 0);
        }

        [AvaloniaFact]
        public void Combined_Horizontal_And_Vertical_Offset()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(1000, 2000), new Size(400, 500));

            // Act
            presenter.Offset = new Vector(300, 800);

            // Assert
            Assert.Equal(300, presenter.Offset.X);
            Assert.Equal(800, presenter.Offset.Y);
        }

        [AvaloniaFact]
        public void SyncOffset_Updates_Both_Dimensions()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            presenter.UpdateScrollInfo(new Size(1000, 2000), new Size(400, 500));

            // Act
            presenter.SyncOffset(150, 600);

            // Assert
            Assert.Equal(150, presenter.Offset.X);
            Assert.Equal(600, presenter.Offset.Y);
        }

        #endregion

        #region Feature Flag Tests

        [AvaloniaFact]
        public void IsLogicalScrollEnabled_Returns_False_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act
            bool result = presenter.IsLogicalScrollEnabled;

            // Assert - without OwningGrid, defaults to false
            Assert.False(result);
        }

        #endregion

        #region GetControlInDirection Extended Tests

        [AvaloniaTheory]
        [InlineData(NavigationDirection.Down)]
        [InlineData(NavigationDirection.Up)]
        [InlineData(NavigationDirection.Left)]
        [InlineData(NavigationDirection.Right)]
        [InlineData(NavigationDirection.First)]
        [InlineData(NavigationDirection.Last)]
        [InlineData(NavigationDirection.PageDown)]
        [InlineData(NavigationDirection.PageUp)]
        public void GetControlInDirection_Handles_All_Directions_Without_OwningGrid(NavigationDirection direction)
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var scrollable = (ILogicalScrollable)presenter;

            // Act - should not throw for any direction
            var result = scrollable.GetControlInDirection(direction, null);

            // Assert - without OwningGrid, all directions return null
            Assert.Null(result);
        }

        #endregion

        #region IScrollAnchorProvider Tests

        [AvaloniaFact]
        public void DataGridRowsPresenter_Implements_IScrollAnchorProvider()
        {
            // Arrange & Act
            var presenter = new DataGridRowsPresenter();

            // Assert
            Assert.IsAssignableFrom<IScrollAnchorProvider>(presenter);
        }

        [AvaloniaFact]
        public void CurrentAnchor_Returns_Null_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var anchorProvider = (IScrollAnchorProvider)presenter;

            // Act
            var result = anchorProvider.CurrentAnchor;

            // Assert
            Assert.Null(result);
        }

        [AvaloniaFact]
        public void RegisterAnchorCandidate_Does_Not_Throw_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var anchorProvider = (IScrollAnchorProvider)presenter;
            var row = new DataGridRow();

            // Act & Assert - should not throw
            anchorProvider.RegisterAnchorCandidate(row);
        }

        [AvaloniaFact]
        public void UnregisterAnchorCandidate_Does_Not_Throw_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var anchorProvider = (IScrollAnchorProvider)presenter;
            var row = new DataGridRow();

            // Act & Assert - should not throw
            anchorProvider.UnregisterAnchorCandidate(row);
        }

        [AvaloniaFact]
        public void CurrentAnchor_Tracks_First_Displayed_Row()
        {
            var items = new[] { 1, 2, 3, 4, 5 };

            var root = new Window
            {
                Width = 400,
                Height = 300,
            };

            root.SetThemeStyles();

            var grid = new DataGrid
            {
                UseLogicalScrollable = true,
                AutoGenerateColumns = false,
                ItemsSource = items
            };

            grid.ColumnsInternal.Add(new DataGridTextColumn
            {
                Header = "Value",
                Binding = new Binding(".")
            });

            root.Content = grid;
            root.Show();

            grid.UpdateLayout();

            var presenter = grid.GetVisualDescendants().OfType<DataGridRowsPresenter>().FirstOrDefault();

            Assert.NotNull(presenter);
            Assert.IsType<DataGridRow>(presenter!.CurrentAnchor);
        }

        #endregion

        #region IScrollSnapPointsInfo Tests

        [AvaloniaFact]
        public void DataGridRowsPresenter_Implements_IScrollSnapPointsInfo()
        {
            // Arrange & Act
            var presenter = new DataGridRowsPresenter();

            // Assert
            Assert.IsAssignableFrom<IScrollSnapPointsInfo>(presenter);
        }

        [AvaloniaFact]
        public void AreHorizontalSnapPointsRegular_Defaults_To_False()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            // Act
            bool result = snapInfo.AreHorizontalSnapPointsRegular;

            // Assert
            Assert.False(result);
        }

        [AvaloniaFact]
        public void AreVerticalSnapPointsRegular_Defaults_To_False()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            // Act
            bool result = snapInfo.AreVerticalSnapPointsRegular;

            // Assert
            Assert.False(result);
        }

        [AvaloniaFact]
        public void AreHorizontalSnapPointsRegular_Can_Be_Set()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            // Act
            snapInfo.AreHorizontalSnapPointsRegular = true;

            // Assert
            Assert.True(snapInfo.AreHorizontalSnapPointsRegular);
        }

        [AvaloniaFact]
        public void AreVerticalSnapPointsRegular_Can_Be_Set()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            // Act
            snapInfo.AreVerticalSnapPointsRegular = true;

            // Assert
            Assert.True(snapInfo.AreVerticalSnapPointsRegular);
        }

        [AvaloniaTheory]
        [InlineData(Orientation.Horizontal)]
        [InlineData(Orientation.Vertical)]
        public void GetIrregularSnapPoints_Returns_Empty_Without_OwningGrid(Orientation orientation)
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            // Act
            var result = snapInfo.GetIrregularSnapPoints(orientation, SnapPointsAlignment.Near);

            // Assert
            Assert.Empty(result);
        }

        [AvaloniaTheory]
        [InlineData(Orientation.Horizontal)]
        [InlineData(Orientation.Vertical)]
        public void GetRegularSnapPoints_Returns_Zero_Without_OwningGrid(Orientation orientation)
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            // Act
            var result = snapInfo.GetRegularSnapPoints(orientation, SnapPointsAlignment.Near, out double offset);

            // Assert
            Assert.Equal(0, result);
            Assert.Equal(0, offset);
        }

        [AvaloniaTheory]
        [InlineData(SnapPointsAlignment.Near)]
        [InlineData(SnapPointsAlignment.Center)]
        [InlineData(SnapPointsAlignment.Far)]
        public void GetIrregularSnapPoints_Handles_All_Alignments_Without_OwningGrid(SnapPointsAlignment alignment)
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            // Act - should not throw
            var horizontalResult = snapInfo.GetIrregularSnapPoints(Orientation.Horizontal, alignment);
            var verticalResult = snapInfo.GetIrregularSnapPoints(Orientation.Vertical, alignment);

            // Assert
            Assert.Empty(horizontalResult);
            Assert.Empty(verticalResult);
        }

        [AvaloniaFact]
        public void GetIrregularSnapPoints_With_OwningGrid_Covers_Vertical_Alignments()
        {
            var grid = new DataGrid { RowHeight = 10 };
            SetPrivateProperty(grid, "SlotCount", 3);

            var presenter = new DataGridRowsPresenter();
            SetPrivateProperty(presenter, "OwningGrid", grid);

            var snapInfo = (IScrollSnapPointsInfo)presenter;

            var near = snapInfo.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near);
            Assert.Equal(new[] { 0d, 10d, 20d }, near);

            var center = snapInfo.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center);
            Assert.Equal(new[] { 5d, 15d, 25d }, center);

            var far = snapInfo.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far);
            Assert.Equal(new[] { 10d, 20d, 30d }, far);

            var defaultAlign = snapInfo.GetIrregularSnapPoints(Orientation.Vertical, (SnapPointsAlignment)999);
            Assert.Equal(new[] { 0d, 10d, 20d }, defaultAlign);
        }

        [AvaloniaFact]
        public void GetIrregularSnapPoints_With_OwningGrid_Covers_Horizontal_Alignments()
        {
            var grid = new DataGrid();
            var first = new DataGridTextColumn { Width = new DataGridLength(20) };
            var second = new DataGridTextColumn { Width = new DataGridLength(30) };
            grid.ColumnsInternal.Add(first);
            grid.ColumnsInternal.Add(second);
            foreach (var column in grid.ColumnsInternal)
            {
                column.SetWidthDisplayValue(column.Width.Value);
            }
            grid.ColumnsInternal.EnsureVisibleEdgedColumnsWidth();

            var presenter = new DataGridRowsPresenter();
            SetPrivateProperty(presenter, "OwningGrid", grid);
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            var near = snapInfo.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near);
            Assert.Equal(new[] { 0d, 20d }, near);

            var center = snapInfo.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center);
            Assert.Equal(new[] { 10d, 35d }, center);

            var far = snapInfo.GetIrregularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far);
            Assert.Equal(new[] { 20d, 50d }, far);

            var defaultAlign = snapInfo.GetIrregularSnapPoints(Orientation.Horizontal, (SnapPointsAlignment)999);
            Assert.Equal(new[] { 0d, 20d }, defaultAlign);
        }

        [AvaloniaFact]
        public void GetRegularSnapPoints_Covers_Vertical_And_Estimates()
        {
            var grid = new DataGrid { RowHeight = 12 };
            var presenter = new DataGridRowsPresenter();
            SetPrivateProperty(presenter, "OwningGrid", grid);
            presenter.AreVerticalSnapPointsRegular = true;
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            var near = snapInfo.GetRegularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near, out var offsetNear);
            Assert.Equal(12, near);
            Assert.Equal(0, offsetNear);

            var center = snapInfo.GetRegularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center, out var offsetCenter);
            Assert.Equal(12, center);
            Assert.Equal(6, offsetCenter);

            var far = snapInfo.GetRegularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far, out var offsetFar);
            Assert.Equal(12, far);
            Assert.Equal(12, offsetFar);

            grid.RowHeight = 0;
            SetPrivateProperty(grid, "RowHeightEstimate", 20d);
            var estimated = snapInfo.GetRegularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near, out var offsetEstimated);
            Assert.Equal(20, estimated);
            Assert.Equal(0, offsetEstimated);
        }

        [AvaloniaFact]
        public void GetRegularSnapPoints_Covers_Horizontal_And_Empty_Scrolling()
        {
            var grid = new DataGrid();
            var column = new DataGridTextColumn { Width = new DataGridLength(30) };
            grid.ColumnsInternal.Add(column);
            column.SetWidthDisplayValue(30);
            grid.ColumnsInternal.EnsureVisibleEdgedColumnsWidth();

            var presenter = new DataGridRowsPresenter();
            SetPrivateProperty(presenter, "OwningGrid", grid);
            presenter.AreHorizontalSnapPointsRegular = true;
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            var width = snapInfo.GetRegularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Center, out var offset);
            Assert.Equal(30, width);
            Assert.Equal(15, offset);

            var near = snapInfo.GetRegularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near, out var offsetNear);
            Assert.Equal(30, near);
            Assert.Equal(0, offsetNear);

            var far = snapInfo.GetRegularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Far, out var offsetFar);
            Assert.Equal(30, far);
            Assert.Equal(30, offsetFar);

            var invalid = snapInfo.GetRegularSnapPoints(Orientation.Horizontal, (SnapPointsAlignment)999, out var offsetInvalid);
            Assert.Equal(30, invalid);
            Assert.Equal(0, offsetInvalid);

            grid.FrozenColumnCount = 1;
            var none = snapInfo.GetRegularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near, out var offsetNone);
            Assert.Equal(0, none);
            Assert.Equal(0, offsetNone);
        }

        [AvaloniaFact]
        public void GetRegularSnapPoints_Horizontal_Returns_Zero_When_Not_Regular()
        {
            var grid = new DataGrid();
            var column = new DataGridTextColumn { Width = new DataGridLength(25) };
            grid.ColumnsInternal.Add(column);
            column.SetWidthDisplayValue(25);
            grid.ColumnsInternal.EnsureVisibleEdgedColumnsWidth();

            var presenter = new DataGridRowsPresenter();
            SetPrivateProperty(presenter, "OwningGrid", grid);
            presenter.AreHorizontalSnapPointsRegular = false;

            var snapInfo = (IScrollSnapPointsInfo)presenter;
            var width = snapInfo.GetRegularSnapPoints(Orientation.Horizontal, SnapPointsAlignment.Near, out var offset);
            Assert.Equal(0, width);
            Assert.Equal(0, offset);
        }

        [AvaloniaFact]
        public void GetRegularSnapPoints_Vertical_Returns_Zero_When_Not_Regular()
        {
            var grid = new DataGrid { RowHeight = 10 };
            var presenter = new DataGridRowsPresenter();
            SetPrivateProperty(presenter, "OwningGrid", grid);
            presenter.AreVerticalSnapPointsRegular = false;

            var snapInfo = (IScrollSnapPointsInfo)presenter;
            var height = snapInfo.GetRegularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near, out var offset);
            Assert.Equal(0, height);
            Assert.Equal(0, offset);
        }

        [AvaloniaFact]
        public void GetVerticalSnapPoints_Uses_Estimate_When_RowHeight_Zero()
        {
            var grid = new DataGrid { RowHeight = 0 };
            SetPrivateProperty(grid, "RowHeightEstimate", 18d);
            SetPrivateProperty(grid, "SlotCount", 2);

            var presenter = new DataGridRowsPresenter();
            SetPrivateProperty(presenter, "OwningGrid", grid);
            var snapInfo = (IScrollSnapPointsInfo)presenter;

            var points = snapInfo.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center);
            Assert.Equal(new[] { 9d, 27d }, points);
        }

        [AvaloniaFact]
        public void GetVerticalSnapPoints_Returns_Empty_When_OwningGrid_Null()
        {
            var presenter = new DataGridRowsPresenter();
            var method = typeof(DataGridRowsPresenter).GetMethod("GetVerticalSnapPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var points = (IReadOnlyList<double>)method!.Invoke(presenter, new object[] { SnapPointsAlignment.Near })!;
            Assert.Empty(points);
        }

        [AvaloniaFact]
        public void GetHorizontalSnapPoints_Returns_Empty_When_OwningGrid_Null()
        {
            var presenter = new DataGridRowsPresenter();
            var method = typeof(DataGridRowsPresenter).GetMethod("GetHorizontalSnapPoints", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            var points = (IReadOnlyList<double>)method!.Invoke(presenter, new object[] { SnapPointsAlignment.Near })!;
            Assert.Empty(points);
        }

        [AvaloniaFact]
        public void SnapPointsChanged_Events_Can_Be_Raised()
        {
            var presenter = new DataGridRowsPresenter();

            presenter.RaiseVerticalSnapPointsChanged();
            presenter.RaiseHorizontalSnapPointsChanged();
        }

        #endregion

        #region Pre-fetching Tests

        [AvaloniaFact]
        public void SchedulePrefetch_Does_Not_Throw_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act & Assert - should not throw
            presenter.SchedulePrefetch();
        }

        [AvaloniaFact]
        public void CancelPrefetch_Does_Not_Throw_Without_OwningGrid()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act & Assert - should not throw
            presenter.CancelPrefetch();
        }

        [AvaloniaFact]
        public void SchedulePrefetch_Can_Be_Called_Multiple_Times()
        {
            // Arrange
            var presenter = new DataGridRowsPresenter();

            // Act & Assert - should not throw when called multiple times
            presenter.SchedulePrefetch();
            presenter.SchedulePrefetch();
            presenter.SchedulePrefetch();
            presenter.CancelPrefetch();
        }

        #endregion

        private static void SetPrivateProperty(object target, string name, object? value)
        {
            var type = target.GetType();
            PropertyInfo? property = null;
            while (type != null)
            {
                property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (property != null)
                {
                    break;
                }
                type = type.BaseType;
            }

            Assert.NotNull(property);
            var setter = property!.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(target, new[] { value });
                return;
            }

            var field = target.GetType().GetField($"<{property.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field!.SetValue(target, value);
        }
    }
}
