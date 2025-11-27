using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Avalonia.Controls;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class ScrollInteractionsViewModel : ObservableObject
    {
        private readonly Random _random = new Random(7);
        private readonly string[] _sources = { "Telemetry", "Orders", "Billing", "Jobs", "Devices", "Sync" };
        private readonly string[] _verbs = { "updated", "failed", "completed", "queued", "succeeded", "timed out" };
        private readonly string[] _targets = { "payload", "batch", "window", "job", "request", "checkpoint" };
        private readonly string[] _detailLines =
        {
            "High latency detected; retry scheduled.",
            "Cache warmed with new segment.",
            "Downstream dependency responded slowly.",
            "User triggered manual refresh.",
            "Backoff jitter adjusted for this run.",
            "Stream emitted sparse data; applying smoothing.",
            "Metrics flushed to storage.",
            "Checkpoint promoted to stable.",
            "Shard ownership changed; rebalancing."
        };

        private int _nextId = 1;
        private LiveDataItem? _selectedItem;
        private bool _enableSnapPoints = true;
        private DataGridRowDetailsVisibilityMode _detailsMode = DataGridRowDetailsVisibilityMode.VisibleWhenSelected;
        private string _statusText = "Items: 0";

        public ScrollInteractionsViewModel()
        {
            Items = new ObservableCollection<LiveDataItem>();
            Items.CollectionChanged += (_, _) => UpdateStatus();

            DetailsModes = new[]
            {
                DataGridRowDetailsVisibilityMode.Collapsed,
                DataGridRowDetailsVisibilityMode.VisibleWhenSelected,
                DataGridRowDetailsVisibilityMode.Visible
            };

            InsertAtTopCommand = new RelayCommand(_ => InsertAtTop());
            AppendCommand = new RelayCommand(_ => AppendToBottom());
            StretchDetailsCommand = new RelayCommand(_ => StretchDetails());
            ResetCommand = new RelayCommand(_ => Reset());

            Reset();
        }

        public ObservableCollection<LiveDataItem> Items { get; }

        public IReadOnlyList<DataGridRowDetailsVisibilityMode> DetailsModes { get; }

        public LiveDataItem? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                    UpdateStatus();
            }
        }

        public bool EnableSnapPoints
        {
            get => _enableSnapPoints;
            set
            {
                if (SetProperty(ref _enableSnapPoints, value))
                    UpdateStatus();
            }
        }

        public DataGridRowDetailsVisibilityMode DetailsMode
        {
            get => _detailsMode;
            set => SetProperty(ref _detailsMode, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public ICommand InsertAtTopCommand { get; }

        public ICommand AppendCommand { get; }

        public ICommand StretchDetailsCommand { get; }

        public ICommand ResetCommand { get; }

        private void Reset()
        {
            Items.Clear();

            foreach (var item in CreateBatch(200, emphasizeDetails: false))
            {
                Items.Add(item);
            }

            UpdateStatus();
        }

        private void InsertAtTop()
        {
            var batch = CreateBatch(10, emphasizeDetails: true);

            for (int i = batch.Count - 1; i >= 0; i--)
            {
                Items.Insert(0, batch[i]);
            }

            UpdateStatus();
        }

        private void AppendToBottom()
        {
            foreach (var item in CreateBatch(10, emphasizeDetails: false))
            {
                Items.Add(item);
            }

            UpdateStatus();
        }

        private void StretchDetails()
        {
            if (Items.Count == 0)
                return;

            int edits = Math.Min(6, Items.Count);
            for (int i = 0; i < edits; i++)
            {
                int index = _random.Next(Items.Count);
                var item = Items[index];
                item.Details = BuildDetails(_random.Next(3, 9));
                item.Timestamp = DateTime.Now;
                item.Summary = BuildSummary(item.Severity);
            }

            UpdateStatus();
        }

        private List<LiveDataItem> CreateBatch(int count, bool emphasizeDetails)
        {
            var list = new List<LiveDataItem>(count);

            for (int i = 0; i < count; i++)
            {
                bool longDetail = emphasizeDetails && i % 3 == 0;
                list.Add(CreateItem(longDetail));
            }

            return list;
        }

        private LiveDataItem CreateItem(bool longDetails)
        {
            string severity = _severities[_random.Next(_severities.Length)];
            var item = new LiveDataItem
            {
                Id = _nextId,
                Title = $"{severity} item {_nextId}",
                Source = _sources[_random.Next(_sources.Length)],
                Severity = severity,
                Accent = MapSeverityToAccent(severity),
                Summary = BuildSummary(severity),
                Details = BuildDetails(longDetails ? _random.Next(4, 9) : _random.Next(1, 5)),
                Timestamp = DateTime.Now,
                IsPinned = _random.NextDouble() < 0.08
            };

            _nextId++;
            return item;
        }

        private string BuildSummary(string severity)
        {
            string verb = _verbs[_random.Next(_verbs.Length)];
            string target = _targets[_random.Next(_targets.Length)];
            return $"{severity}: {verb} {target}";
        }

        private string BuildDetails(int lines)
        {
            var builder = new StringBuilder();

            for (int i = 0; i < lines; i++)
            {
                builder.Append("• ");
                builder.Append(_detailLines[_random.Next(_detailLines.Length)]);

                if (i < lines - 1)
                    builder.AppendLine();
            }

            return builder.ToString();
        }

        private string MapSeverityToAccent(string severity) => severity switch
        {
            "Critical" => "#D32F2F",
            "Error" => "#EF5350",
            "Warning" => "#FBC02D",
            "Info" => "#42A5F5",
            _ => "#4CAF50"
        };

        private void UpdateStatus()
        {
            StatusText = $"Items: {Items.Count:n0} | Selected: {(SelectedItem?.Title ?? "None")} | Snap: {(EnableSnapPoints ? "On" : "Off")}";
        }

        private readonly string[] _severities = { "Info", "Warning", "Error", "Critical" };
    }
}
