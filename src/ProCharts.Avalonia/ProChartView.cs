// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Utilities;
using ProCharts;
using ProCharts.Skia;
using SkiaSharp;

namespace ProCharts.Avalonia
{
    public sealed class ProChartView : Control
    {
        public static readonly StyledProperty<ChartModel?> ChartModelProperty =
            AvaloniaProperty.Register<ProChartView, ChartModel?>(nameof(ChartModel));

        public static readonly StyledProperty<SkiaChartStyle?> ChartStyleProperty =
            AvaloniaProperty.Register<ProChartView, SkiaChartStyle?>(nameof(ChartStyle));

        public static readonly StyledProperty<bool> ShowToolTipsProperty =
            AvaloniaProperty.Register<ProChartView, bool>(nameof(ShowToolTips), true);

        public static readonly StyledProperty<Func<SkiaChartHitTestResult, string>?> ToolTipFormatterProperty =
            AvaloniaProperty.Register<ProChartView, Func<SkiaChartHitTestResult, string>?>(nameof(ToolTipFormatter));

        public static readonly StyledProperty<bool> EnablePanZoomProperty =
            AvaloniaProperty.Register<ProChartView, bool>(nameof(EnablePanZoom), true);

        public static readonly StyledProperty<MouseButton> PanButtonProperty =
            AvaloniaProperty.Register<ProChartView, MouseButton>(nameof(PanButton), MouseButton.Left);

        public static readonly StyledProperty<KeyModifiers> PanModifiersProperty =
            AvaloniaProperty.Register<ProChartView, KeyModifiers>(nameof(PanModifiers), KeyModifiers.None);

        public static readonly StyledProperty<KeyModifiers> ZoomModifiersProperty =
            AvaloniaProperty.Register<ProChartView, KeyModifiers>(nameof(ZoomModifiers), KeyModifiers.Control);

        public static readonly StyledProperty<double> ZoomStepProperty =
            AvaloniaProperty.Register<ProChartView, double>(nameof(ZoomStep), 0.2d);

        public static readonly StyledProperty<int> MinWindowCountProperty =
            AvaloniaProperty.Register<ProChartView, int>(nameof(MinWindowCount), 10);

        private readonly SkiaChartRenderer _renderer = new();
        private readonly SkiaChartRenderCache _renderCache = new();
        private WriteableBitmap? _bitmap;
        private Size _lastSize;
        private double _lastScaling = 1d;
        private bool _isDirty = true;
        private SkiaChartHitTestResult? _lastHit;
        private string? _lastToolTipText;
        private bool _isPanning;
        private Point _panStartPoint;
        private int _panStartWindowStart;
        private int _panStartWindowCount;
        private int _panTotalCount;
        private SkiaChartViewportInfo _panViewport;
        private bool _hasPanViewport;
        private IPointer? _panPointer;

        static ProChartView()
        {
            AffectsRender<ProChartView>(ChartModelProperty, ChartStyleProperty);
        }

        public ChartModel? ChartModel
        {
            get => GetValue(ChartModelProperty);
            set => SetValue(ChartModelProperty, value);
        }

        public SkiaChartStyle? ChartStyle
        {
            get => GetValue(ChartStyleProperty);
            set => SetValue(ChartStyleProperty, value);
        }

        public bool ShowToolTips
        {
            get => GetValue(ShowToolTipsProperty);
            set => SetValue(ShowToolTipsProperty, value);
        }

        public Func<SkiaChartHitTestResult, string>? ToolTipFormatter
        {
            get => GetValue(ToolTipFormatterProperty);
            set => SetValue(ToolTipFormatterProperty, value);
        }

        public bool EnablePanZoom
        {
            get => GetValue(EnablePanZoomProperty);
            set => SetValue(EnablePanZoomProperty, value);
        }

        public MouseButton PanButton
        {
            get => GetValue(PanButtonProperty);
            set => SetValue(PanButtonProperty, value);
        }

        public KeyModifiers PanModifiers
        {
            get => GetValue(PanModifiersProperty);
            set => SetValue(PanModifiersProperty, value);
        }

        public KeyModifiers ZoomModifiers
        {
            get => GetValue(ZoomModifiersProperty);
            set => SetValue(ZoomModifiersProperty, value);
        }

        public double ZoomStep
        {
            get => GetValue(ZoomStepProperty);
            set => SetValue(ZoomStepProperty, value);
        }

        public int MinWindowCount
        {
            get => GetValue(MinWindowCountProperty);
            set => SetValue(MinWindowCountProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ChartModelProperty)
            {
                if (change.OldValue is ChartModel oldModel)
                {
                    WeakEventHandlerManager.Unsubscribe<ChartDataUpdateEventArgs, ProChartView>(
                        oldModel,
                        nameof(ChartModel.SnapshotUpdated),
                        OnSnapshotUpdated);
                    WeakEventHandlerManager.Unsubscribe<PropertyChangedEventArgs, ProChartView>(
                        oldModel,
                        nameof(INotifyPropertyChanged.PropertyChanged),
                        OnChartModelPropertyChanged);
                }

                if (change.NewValue is ChartModel newModel)
                {
                    WeakEventHandlerManager.Subscribe<ChartModel, ChartDataUpdateEventArgs, ProChartView>(
                        newModel,
                        nameof(ChartModel.SnapshotUpdated),
                        OnSnapshotUpdated);
                    WeakEventHandlerManager.Subscribe<ChartModel, PropertyChangedEventArgs, ProChartView>(
                        newModel,
                        nameof(INotifyPropertyChanged.PropertyChanged),
                        OnChartModelPropertyChanged);
                }

                _isDirty = true;
                EndPan();
                ClearToolTip();
                InvalidateVisual();
            }
            else if (change.Property == ShowToolTipsProperty)
            {
                if (!ShowToolTips)
                {
                    ClearToolTip();
                }
            }
            else if (change.Property == ToolTipFormatterProperty)
            {
                UpdateToolTipText();
            }
            else if (change.Property == ChartStyleProperty)
            {
                _isDirty = true;
                EndPan();
                ClearToolTip();
                InvalidateVisual();
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            EnsureBitmap();
            if (_bitmap == null)
            {
                return;
            }

            if (_isDirty)
            {
                RenderToBitmap();
            }

            using (context.PushRenderOptions(new RenderOptions { BitmapInterpolationMode = BitmapInterpolationMode.None }))
            {
                var sourceRect = new Rect(0, 0, _bitmap.PixelSize.Width, _bitmap.PixelSize.Height);
                context.DrawImage(_bitmap, sourceRect, new Rect(Bounds.Size));
            }
        }

        private void OnSnapshotUpdated(object? sender, ChartDataUpdateEventArgs e)
        {
            if (e.Update.Delta.Kind == ChartDataDeltaKind.None)
            {
                EnsureWindowBounds();
                return;
            }

            _isDirty = true;
            ClearToolTip();
            EnsureWindowBounds();
            InvalidateVisual();
        }

        private void OnChartModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            _isDirty = true;
            ClearToolTip();
            InvalidateVisual();
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_isPanning)
            {
                UpdatePan(e.GetPosition(this));
                return;
            }

            UpdateToolTip(e.GetPosition(this));
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            ClearToolTip();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!EnablePanZoom)
            {
                return;
            }

            if (!IsPanGesture(e))
            {
                return;
            }

            var point = e.GetCurrentPoint(this);
            if (!TryBeginPan(point))
            {
                return;
            }

            _panPointer = e.Pointer;
            e.Pointer.Capture(this);
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (_isPanning && (_panPointer == null || ReferenceEquals(e.Pointer, _panPointer)))
            {
                EndPan();
                e.Handled = true;
            }
        }

        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            EndPan();
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (!EnablePanZoom || !IsZoomGesture(e))
            {
                return;
            }

            if (!TryGetWindowState(out var model, out var total, out var start, out var count))
            {
                return;
            }

            var step = Math.Max(0.01d, ZoomStep);
            var scale = Math.Pow(1d + step, e.Delta.Y);
            if (Math.Abs(scale - 1d) < 0.0001d)
            {
                return;
            }

            var newCount = (int)Math.Round(count / scale);
            var minCount = Math.Max(1, Math.Min(MinWindowCount, total));
            newCount = Math.Max(minCount, Math.Min(newCount, total));

            if (!TryGetViewportInfo(model, out var viewport) || !viewport.HasCartesianSeries)
            {
                return;
            }

            var ratio = GetAxisRatio(viewport, e.GetPosition(this));

            var anchorIndex = start + (int)Math.Round(ratio * Math.Max(0, count - 1));
            var newStart = anchorIndex - (int)Math.Round(ratio * Math.Max(0, newCount - 1));

            ApplyWindow(model, total, newStart, newCount);
            ClearToolTip();
            e.Handled = true;
        }

        private void EnsureBitmap()
        {
            var size = Bounds.Size;
            var scaling = VisualRoot?.RenderScaling ?? 1d;
            if (_bitmap != null && size.Equals(_lastSize) && Math.Abs(_lastScaling - scaling) < double.Epsilon)
            {
                return;
            }

            _lastSize = size;
            _lastScaling = scaling;

            var pixelWidth = Math.Max(1, (int)Math.Round(size.Width * scaling));
            var pixelHeight = Math.Max(1, (int)Math.Round(size.Height * scaling));
            var pixelSize = new PixelSize(pixelWidth, pixelHeight);
            var dpiX = size.Width > 0 ? (pixelWidth / size.Width) * 96 : 96;
            var dpiY = size.Height > 0 ? (pixelHeight / size.Height) * 96 : 96;
            var dpi = new Vector(dpiX, dpiY);

            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(pixelSize, dpi, PixelFormat.Bgra8888, AlphaFormat.Premul);
            _isDirty = true;
        }

        private void RenderToBitmap()
        {
            if (_bitmap == null)
            {
                return;
            }

            using var locked = _bitmap.Lock();
            var info = new SKImageInfo(locked.Size.Width, locked.Size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info, locked.Address, locked.RowBytes);
            if (surface == null)
            {
                return;
            }

            var model = ChartModel;
            var snapshot = model?.Snapshot ?? ChartDataSnapshot.Empty;
            var update = model?.LastUpdate;
            var style = BuildEffectiveStyle();
            var canvas = surface.Canvas;
            canvas.Save();
            var scaleX = _lastSize.Width > 0 ? (float)(locked.Size.Width / _lastSize.Width) : 1f;
            var scaleY = _lastSize.Height > 0 ? (float)(locked.Size.Height / _lastSize.Height) : 1f;
            if (Math.Abs(scaleX - 1f) > float.Epsilon || Math.Abs(scaleY - 1f) > float.Epsilon)
            {
                canvas.Scale(scaleX, scaleY);
            }

            var rect = new SKRect(0, 0, (float)_lastSize.Width, (float)_lastSize.Height);
            if (update != null)
            {
                _renderer.Render(canvas, rect, update, style, _renderCache);
            }
            else
            {
                _renderer.Render(canvas, rect, snapshot, style, _renderCache);
            }
            canvas.Restore();
            canvas.Flush();

            _isDirty = false;
        }

        private SkiaChartStyle BuildEffectiveStyle()
        {
            var style = ChartStyle != null ? new SkiaChartStyle(ChartStyle) : new SkiaChartStyle();
            var model = ChartModel;
            if (model == null)
            {
                return style;
            }

            var legend = model.Legend;
            style.LegendPosition = legend.Position;
            style.ShowLegend = legend.IsVisible && legend.Position != ChartLegendPosition.None;

            var categoryAxis = model.CategoryAxis;
            style.ShowCategoryLabels = categoryAxis.IsVisible;
            style.ShowCategoryAxisLine = categoryAxis.IsVisible;
            style.CategoryAxisTitle = categoryAxis.IsVisible ? categoryAxis.Title : null;
            style.CategoryAxisKind = categoryAxis.Kind;
            style.CategoryAxisMinimum = categoryAxis.Minimum;
            style.CategoryAxisMaximum = categoryAxis.Maximum;
            style.CategoryAxisLabelFormatter = categoryAxis.LabelFormatter;
            style.CategoryAxisCrossing = categoryAxis.Crossing;
            style.CategoryAxisCrossingValue = categoryAxis.CrossingValue;
            style.CategoryAxisOffset = categoryAxis.Offset;
            style.CategoryAxisMinorTickCount = categoryAxis.MinorTickCount;
            style.ShowCategoryMinorTicks = categoryAxis.ShowMinorTicks;
            style.ShowCategoryMinorGridlines = categoryAxis.ShowMinorGridlines;

            var valueAxis = model.ValueAxis;
            style.ShowAxisLabels = valueAxis.IsVisible;
            style.ShowValueAxisLine = valueAxis.IsVisible;
            style.ValueAxisTitle = valueAxis.IsVisible ? valueAxis.Title : null;
            style.ValueAxisMinimum = valueAxis.Minimum;
            style.ValueAxisMaximum = valueAxis.Maximum;
            style.ValueAxisKind = valueAxis.Kind;
            style.AxisLabelFormatter = valueAxis.LabelFormatter;
            style.ValueAxisCrossing = valueAxis.Crossing;
            style.ValueAxisCrossingValue = valueAxis.CrossingValue;
            style.ValueAxisOffset = valueAxis.Offset;
            style.ValueAxisMinorTickCount = valueAxis.MinorTickCount;
            style.ShowValueMinorTicks = valueAxis.ShowMinorTicks;
            style.ShowValueMinorGridlines = valueAxis.ShowMinorGridlines;

            var secondaryAxis = model.SecondaryValueAxis;
            style.ShowSecondaryValueAxis = secondaryAxis.IsVisible;
            style.SecondaryValueAxisTitle = secondaryAxis.IsVisible ? secondaryAxis.Title : null;
            style.SecondaryValueAxisMinimum = secondaryAxis.Minimum;
            style.SecondaryValueAxisMaximum = secondaryAxis.Maximum;
            style.SecondaryValueAxisKind = secondaryAxis.Kind;
            style.SecondaryAxisLabelFormatter = secondaryAxis.LabelFormatter;
            style.SecondaryValueAxisCrossing = secondaryAxis.Crossing;
            style.SecondaryValueAxisCrossingValue = secondaryAxis.CrossingValue;
            style.SecondaryValueAxisOffset = secondaryAxis.Offset;
            style.SecondaryValueAxisMinorTickCount = secondaryAxis.MinorTickCount;
            style.ShowSecondaryValueMinorTicks = secondaryAxis.ShowMinorTicks;
            style.ShowSecondaryValueMinorGridlines = secondaryAxis.ShowMinorGridlines;

            var secondaryCategoryAxis = model.SecondaryCategoryAxis;
            style.ShowSecondaryCategoryAxis = secondaryCategoryAxis.IsVisible;
            style.SecondaryCategoryAxisTitle = secondaryCategoryAxis.IsVisible ? secondaryCategoryAxis.Title : null;
            style.SecondaryCategoryAxisKind = secondaryCategoryAxis.Kind;
            style.SecondaryCategoryAxisMinimum = secondaryCategoryAxis.Minimum;
            style.SecondaryCategoryAxisMaximum = secondaryCategoryAxis.Maximum;
            style.SecondaryCategoryAxisLabelFormatter = secondaryCategoryAxis.LabelFormatter;
            style.SecondaryCategoryAxisCrossing = secondaryCategoryAxis.Crossing;
            style.SecondaryCategoryAxisCrossingValue = secondaryCategoryAxis.CrossingValue;
            style.SecondaryCategoryAxisOffset = secondaryCategoryAxis.Offset;
            style.SecondaryCategoryAxisMinorTickCount = secondaryCategoryAxis.MinorTickCount;
            style.ShowSecondaryCategoryMinorTicks = secondaryCategoryAxis.ShowMinorTicks;
            style.ShowSecondaryCategoryMinorGridlines = secondaryCategoryAxis.ShowMinorGridlines;

            style.CoreTheme = model.Theme;
            style.CoreSeriesStyles = model.SeriesStyles;

            return style;
        }

        public byte[] ExportPng()
        {
            if (!TryGetExportSizes(out var pixelWidth, out var pixelHeight, out _, out _))
            {
                return Array.Empty<byte>();
            }

            var snapshot = ChartModel?.Snapshot ?? ChartDataSnapshot.Empty;
            var style = BuildEffectiveStyle();
            return SkiaChartExporter.ExportPng(snapshot, pixelWidth, pixelHeight, style);
        }

        public string ExportSvg()
        {
            if (!TryGetExportSizes(out _, out _, out var width, out var height))
            {
                return string.Empty;
            }

            var snapshot = ChartModel?.Snapshot ?? ChartDataSnapshot.Empty;
            var style = BuildEffectiveStyle();
            return SkiaChartExporter.ExportSvg(snapshot, width, height, style);
        }

        public Task CopyToClipboardAsync(ChartClipboardFormat format)
        {
            return format switch
            {
                ChartClipboardFormat.Png => CopyPngToClipboardAsync(),
                ChartClipboardFormat.Svg => CopySvgToClipboardAsync(),
                _ => Task.CompletedTask
            };
        }

        private async Task CopyPngToClipboardAsync()
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
            {
                return;
            }

            var png = ExportPng();
            if (png.Length == 0)
            {
                return;
            }

            using var stream = new MemoryStream(png);
            using var bitmap = new Bitmap(stream);
            await clipboard.SetBitmapAsync(bitmap);
        }

        private async Task CopySvgToClipboardAsync()
        {
            if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
            {
                return;
            }

            var svg = ExportSvg();
            if (string.IsNullOrWhiteSpace(svg))
            {
                return;
            }

            var item = new DataTransferItem();
            item.Set(DataFormat.Text, svg);
            item.Set(ChartClipboardFormats.Svg, svg);

            var dataTransfer = new DataTransfer();
            dataTransfer.Add(item);

            await clipboard.SetDataAsync(dataTransfer);
        }

        private bool TryGetExportSizes(
            out int pixelWidth,
            out int pixelHeight,
            out int width,
            out int height)
        {
            var size = Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0)
            {
                pixelWidth = 0;
                pixelHeight = 0;
                width = 0;
                height = 0;
                return false;
            }

            var scaling = VisualRoot?.RenderScaling ?? 1d;
            pixelWidth = Math.Max(1, (int)Math.Round(size.Width * scaling));
            pixelHeight = Math.Max(1, (int)Math.Round(size.Height * scaling));
            width = Math.Max(1, (int)Math.Round(size.Width));
            height = Math.Max(1, (int)Math.Round(size.Height));
            return true;
        }

        private void UpdateToolTip(Point point)
        {
            if (!ShowToolTips)
            {
                ClearToolTip();
                return;
            }

            var model = ChartModel;
            if (model == null)
            {
                ClearToolTip();
                return;
            }

            var snapshot = model.Snapshot;
            if (snapshot.Series.Count == 0 || snapshot.Categories.Count == 0)
            {
                ClearToolTip();
                return;
            }

            var hitPoint = new SKPoint((float)point.X, (float)point.Y);
            var bounds = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            var style = BuildEffectiveStyle();
            var hit = _renderer.HitTest(hitPoint, bounds, snapshot, style);

            if (hit.HasValue)
            {
                var hitValue = hit.Value;
                var text = FormatToolTip(hitValue);
                if (!_lastHit.HasValue || !_lastHit.Value.Equals(hitValue) || _lastToolTipText != text)
                {
                    _lastHit = hitValue;
                    _lastToolTipText = text;
                    ToolTip.SetTip(this, text);
                }

                return;
            }

            ClearToolTip();
        }

        private void UpdateToolTipText()
        {
            if (!_lastHit.HasValue)
            {
                return;
            }

            var text = FormatToolTip(_lastHit.Value);
            if (_lastToolTipText != text)
            {
                _lastToolTipText = text;
                ToolTip.SetTip(this, text);
            }
        }

        private string FormatToolTip(SkiaChartHitTestResult hit)
        {
            var formatter = ToolTipFormatter;
            if (formatter != null)
            {
                return formatter(hit);
            }

            var seriesName = string.IsNullOrWhiteSpace(hit.SeriesName)
                ? $"Series {hit.SeriesIndex + 1}"
                : hit.SeriesName!;

            if ((hit.SeriesKind == ChartSeriesKind.Scatter || hit.SeriesKind == ChartSeriesKind.Bubble) && hit.XValue.HasValue)
            {
                return $"{seriesName}: ({FormatValue(hit.XValue.Value)}, {FormatValue(hit.Value)})";
            }

            if (!string.IsNullOrWhiteSpace(hit.Category))
            {
                return $"{seriesName} - {hit.Category}: {FormatValue(hit.Value)}";
            }

            return $"{seriesName}: {FormatValue(hit.Value)}";
        }

        private static string FormatValue(double value)
        {
            return value.ToString("G", CultureInfo.CurrentCulture);
        }

        private void ClearToolTip()
        {
            if (!_lastHit.HasValue && _lastToolTipText == null)
            {
                return;
            }

            _lastHit = null;
            _lastToolTipText = null;
            ToolTip.SetTip(this, null);
        }

        private bool IsPanGesture(PointerPressedEventArgs e)
        {
            var modifiers = e.KeyModifiers;
            if ((modifiers & PanModifiers) != PanModifiers)
            {
                return false;
            }

            var point = e.GetCurrentPoint(this);
            return PanButton switch
            {
                MouseButton.Left => point.Properties.IsLeftButtonPressed,
                MouseButton.Middle => point.Properties.IsMiddleButtonPressed,
                MouseButton.Right => point.Properties.IsRightButtonPressed,
                _ => false
            };
        }

        private bool IsZoomGesture(PointerWheelEventArgs e)
        {
            var modifiers = e.KeyModifiers;
            if ((modifiers & ZoomModifiers) != ZoomModifiers)
            {
                return false;
            }

            return Math.Abs(e.Delta.Y) > double.Epsilon;
        }

        private bool TryBeginPan(PointerPoint point)
        {
            if (!TryGetWindowState(out var model, out var total, out var start, out var count))
            {
                return false;
            }

            if (total <= 0 || count <= 0 || count >= total)
            {
                return false;
            }

            if (!TryGetViewportInfo(model, out var viewport) || !viewport.HasCartesianSeries)
            {
                return false;
            }

            _isPanning = true;
            _panStartPoint = point.Position;
            _panStartWindowStart = start;
            _panStartWindowCount = count;
            _panTotalCount = total;
            _panViewport = viewport;
            _hasPanViewport = true;
            ClearToolTip();
            return true;
        }

        private void UpdatePan(Point position)
        {
            if (!_isPanning || !_hasPanViewport)
            {
                return;
            }

            var model = ChartModel;
            if (model == null)
            {
                return;
            }

            var axisLength = _panViewport.BarOnly ? _panViewport.Plot.Height : _panViewport.Plot.Width;
            if (axisLength <= 0)
            {
                return;
            }

            var deltaPixels = _panViewport.BarOnly
                ? position.Y - _panStartPoint.Y
                : position.X - _panStartPoint.X;
            var deltaRatio = deltaPixels / axisLength;
            var deltaIndex = (int)Math.Round(-deltaRatio * _panStartWindowCount);
            var newStart = _panStartWindowStart + deltaIndex;

            ApplyWindow(model, _panTotalCount, newStart, _panStartWindowCount);
        }

        private void EndPan()
        {
            if (!_isPanning)
            {
                return;
            }

            _isPanning = false;
            _hasPanViewport = false;
            _panStartWindowStart = 0;
            _panStartWindowCount = 0;
            _panTotalCount = 0;
            if (_panPointer != null && ReferenceEquals(_panPointer.Captured, this))
            {
                _panPointer.Capture(null);
            }

            _panPointer = null;
        }

        private bool TryGetWindowState(out ChartModel model, out int total, out int start, out int count)
        {
            model = ChartModel!;
            total = 0;
            start = 0;
            count = 0;

            if (model == null)
            {
                return false;
            }

            total = GetTotalCategoryCount(model);
            if (total <= 0)
            {
                return false;
            }

            start = model.Request.WindowStart ?? 0;
            count = model.Request.WindowCount ?? total;
            if (count <= 0)
            {
                count = total;
            }

            if (count > total)
            {
                count = total;
            }

            if (start < 0)
            {
                start = 0;
            }

            if (start > total)
            {
                start = total;
            }

            if (start + count > total)
            {
                start = Math.Max(0, total - count);
            }

            return true;
        }

        private int GetTotalCategoryCount(ChartModel model)
        {
            if (model.DataSource is IChartWindowInfoProvider provider)
            {
                var count = provider.GetTotalCategoryCount();
                if (count.HasValue)
                {
                    return Math.Max(0, count.Value);
                }
            }

            return model.Snapshot.Categories.Count;
        }

        private void ApplyWindow(ChartModel model, int total, int start, int count)
        {
            if (total <= 0)
            {
                model.Request.WindowStart = null;
                model.Request.WindowCount = null;
                return;
            }

            var minCount = Math.Max(1, Math.Min(MinWindowCount, total));
            var newCount = Math.Max(minCount, Math.Min(count, total));
            var newStart = Math.Max(0, Math.Min(start, Math.Max(0, total - 1)));
            if (newStart + newCount > total)
            {
                newStart = Math.Max(0, total - newCount);
            }

            if (newStart == 0 && newCount >= total)
            {
                model.Request.WindowStart = null;
                model.Request.WindowCount = null;
                return;
            }

            model.Request.WindowStart = newStart;
            model.Request.WindowCount = newCount;
        }

        private void EnsureWindowBounds()
        {
            var model = ChartModel;
            if (model == null)
            {
                return;
            }

            var request = model.Request;
            if (!request.WindowStart.HasValue && !request.WindowCount.HasValue)
            {
                return;
            }

            if (TryGetWindowState(out var resolvedModel, out var total, out var start, out var count))
            {
                ApplyWindow(resolvedModel, total, start, count);
            }
        }

        private bool TryGetViewportInfo(ChartModel model, out SkiaChartViewportInfo viewport)
        {
            var snapshot = model.Snapshot;
            if (snapshot.Series.Count == 0)
            {
                viewport = default;
                return false;
            }

            var bounds = new SKRect(0, 0, (float)Bounds.Width, (float)Bounds.Height);
            var style = BuildEffectiveStyle();
            return _renderer.TryGetViewportInfo(bounds, snapshot, style, out viewport);
        }

        private static double GetAxisRatio(SkiaChartViewportInfo viewport, Point position)
        {
            var axisStart = viewport.BarOnly ? viewport.Plot.Top : viewport.Plot.Left;
            var axisLength = viewport.BarOnly ? viewport.Plot.Height : viewport.Plot.Width;
            if (axisLength <= 0)
            {
                return 0.5d;
            }

            var axisPosition = viewport.BarOnly ? position.Y : position.X;
            var ratio = (axisPosition - axisStart) / axisLength;
            if (ratio < 0d)
            {
                return 0d;
            }

            if (ratio > 1d)
            {
                return 1d;
            }

            return ratio;
        }
    }
}
