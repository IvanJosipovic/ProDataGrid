// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace ProCharts
{
    public sealed class ChartModel : INotifyPropertyChanged, IDisposable
    {
        private IChartDataSource? _dataSource;
        private EventHandler? _dataInvalidatedHandler;
        private ChartDataSnapshot _snapshot = ChartDataSnapshot.Empty;
        private bool _autoRefresh = true;
        private int _updateNesting;
        private bool _pendingRefresh;
        private ChartTheme? _theme;
        private IReadOnlyList<ChartSeriesStyle>? _seriesStyles;

        public ChartModel()
        {
            Request = new ChartDataRequest();
            Request.PropertyChanged += OnRequestChanged;
            CategoryAxis = new ChartAxisDefinition(ChartAxisKind.Category);
            CategoryAxis.PropertyChanged += OnAxisChanged;
            SecondaryCategoryAxis = new ChartAxisDefinition(ChartAxisKind.Category) { IsVisible = false };
            SecondaryCategoryAxis.PropertyChanged += OnAxisChanged;
            ValueAxis = new ChartAxisDefinition(ChartAxisKind.Value);
            ValueAxis.PropertyChanged += OnAxisChanged;
            SecondaryValueAxis = new ChartAxisDefinition(ChartAxisKind.Value) { IsVisible = false };
            SecondaryValueAxis.PropertyChanged += OnAxisChanged;
            Legend = new ChartLegendDefinition();
            Legend.PropertyChanged += OnLegendChanged;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? SnapshotChanged;

        public event EventHandler<ChartDataUpdateEventArgs>? SnapshotUpdated;

        public ChartDataRequest Request { get; }

        public ChartAxisDefinition CategoryAxis { get; }

        public ChartAxisDefinition SecondaryCategoryAxis { get; }

        public ChartAxisDefinition ValueAxis { get; }

        public ChartAxisDefinition SecondaryValueAxis { get; }

        public ChartLegendDefinition Legend { get; }

        public ChartTheme? Theme
        {
            get => _theme;
            set
            {
                if (ReferenceEquals(_theme, value))
                {
                    return;
                }

                _theme = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Theme)));
            }
        }

        public IReadOnlyList<ChartSeriesStyle>? SeriesStyles
        {
            get => _seriesStyles;
            set
            {
                if (ReferenceEquals(_seriesStyles, value))
                {
                    return;
                }

                _seriesStyles = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SeriesStyles)));
            }
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set
            {
                if (_autoRefresh == value)
                {
                    return;
                }

                _autoRefresh = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AutoRefresh)));
                RequestRefresh();
            }
        }

        public IChartDataSource? DataSource
        {
            get => _dataSource;
            set
            {
                if (ReferenceEquals(_dataSource, value))
                {
                    return;
                }

                DetachDataSource();
                _dataSource = value;
                AttachDataSource();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DataSource)));
                RequestRefresh();
            }
        }

        public ChartDataSnapshot Snapshot
        {
            get => _snapshot;
            private set
            {
                if (ReferenceEquals(_snapshot, value))
                {
                    return;
                }

                _snapshot = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Snapshot)));
                SnapshotChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public ChartDataUpdate? LastUpdate { get; private set; }

        public void Refresh()
        {
            var dataSource = _dataSource;
            if (dataSource == null)
            {
                ApplyUpdate(new ChartDataUpdate(ChartDataSnapshot.Empty, ChartDataDelta.Full));
                return;
            }

            if (dataSource is IChartIncrementalDataSource incremental)
            {
                if (incremental.TryBuildUpdate(Request, _snapshot, out var update))
                {
                    ApplyUpdate(update);
                    return;
                }
            }

            ApplyUpdate(new ChartDataUpdate(dataSource.BuildSnapshot(Request), ChartDataDelta.Full));
        }

        public void BeginUpdate()
        {
            _updateNesting++;
        }

        public void EndUpdate()
        {
            if (_updateNesting == 0)
            {
                return;
            }

            _updateNesting--;
            if (_updateNesting == 0 && _pendingRefresh)
            {
                _pendingRefresh = false;
                if (_autoRefresh)
                {
                    Refresh();
                }
            }
        }

        public IDisposable DeferRefresh()
        {
            BeginUpdate();
            return new DeferScope(this);
        }

        public void Dispose()
        {
            DetachDataSource();
            Request.PropertyChanged -= OnRequestChanged;
            CategoryAxis.PropertyChanged -= OnAxisChanged;
            SecondaryCategoryAxis.PropertyChanged -= OnAxisChanged;
            ValueAxis.PropertyChanged -= OnAxisChanged;
            SecondaryValueAxis.PropertyChanged -= OnAxisChanged;
            Legend.PropertyChanged -= OnLegendChanged;
        }

        private void AttachDataSource()
        {
            if (_dataSource == null)
            {
                return;
            }

            _dataInvalidatedHandler ??= CreateDataInvalidatedHandler();
            _dataSource.DataInvalidated += _dataInvalidatedHandler;
        }

        private void DetachDataSource()
        {
            if (_dataSource == null || _dataInvalidatedHandler == null)
            {
                return;
            }

            _dataSource.DataInvalidated -= _dataInvalidatedHandler;
        }

        private EventHandler CreateDataInvalidatedHandler()
        {
            var weakSelf = new WeakReference<ChartModel>(this);
            EventHandler? handler = null;
            handler = (sender, args) =>
            {
                if (!weakSelf.TryGetTarget(out var model))
                {
                    if (sender is IChartDataSource source)
                    {
                        source.DataInvalidated -= handler;
                    }

                    return;
                }

                model.RequestRefresh();
            };
            return handler;
        }

        private void OnRequestChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Request)));
            RequestRefresh();
        }

        private void OnAxisChanged(object? sender, PropertyChangedEventArgs e)
        {
            var propertyName = ReferenceEquals(sender, CategoryAxis)
                ? nameof(CategoryAxis)
                : ReferenceEquals(sender, SecondaryCategoryAxis)
                    ? nameof(SecondaryCategoryAxis)
                    : ReferenceEquals(sender, SecondaryValueAxis)
                        ? nameof(SecondaryValueAxis)
                        : nameof(ValueAxis);

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnLegendChanged(object? sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Legend)));
        }

        private void RequestRefresh()
        {
            if (_updateNesting > 0)
            {
                _pendingRefresh = true;
                return;
            }

            if (_autoRefresh)
            {
                Refresh();
            }
        }

        private void ApplyUpdate(ChartDataUpdate update)
        {
            LastUpdate = update;
            Snapshot = update.Snapshot;
            SnapshotUpdated?.Invoke(this, new ChartDataUpdateEventArgs(update));
        }

        private sealed class DeferScope : IDisposable
        {
            private ChartModel? _model;

            public DeferScope(ChartModel model)
            {
                _model = model;
            }

            public void Dispose()
            {
                var model = _model;
                if (model == null)
                {
                    return;
                }

                _model = null;
                model.EndUpdate();
            }
        }
    }
}
