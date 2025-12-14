using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridDragDrop;
using Avalonia.Input;
using DataGridSample.Models;
using DataGridSample.Mvvm;

namespace DataGridSample.ViewModels
{
    public class RowDragDropViewModel : ObservableObject
    {
        private readonly ObservableCollection<ChangeItem> _items;
        private readonly DataGridRowDragDropOptions _options;
        private readonly IDataGridRowDropHandler _dropHandler;
        private DataGridRowDragHandle _rowDragHandle;
        private bool _allowCopy;
        private bool _showHandle = true;
        private int _nextId;

        public RowDragDropViewModel()
        {
            _items = new ObservableCollection<ChangeItem>(CreateItems());
            _nextId = _items.Any() ? _items.Max(x => x.Id) : 0;
            _options = new DataGridRowDragDropOptions
            {
                AllowedEffects = DragDropEffects.Move
            };
            _dropHandler = new ChangeItemDropHandler(this);
            _rowDragHandle = DataGridRowDragHandle.RowHeaderAndRow;
            DragHandles = new[]
            {
                DataGridRowDragHandle.RowHeader,
                DataGridRowDragHandle.Row,
                DataGridRowDragHandle.RowHeaderAndRow
            };
        }

        public ObservableCollection<ChangeItem> Items => _items;

        public DataGridRowDragDropOptions Options => _options;

        public IDataGridRowDropHandler DropHandler => _dropHandler;

        public IReadOnlyList<DataGridRowDragHandle> DragHandles { get; }

        public DataGridRowDragHandle RowDragHandle
        {
            get => _rowDragHandle;
            set => SetProperty(ref _rowDragHandle, value);
        }

        public bool ShowHandle
        {
            get => _showHandle;
            set => SetProperty(ref _showHandle, value);
        }

        public bool AllowCopy
        {
            get => _allowCopy;
            set
            {
                if (SetProperty(ref _allowCopy, value))
                {
                    _options.AllowedEffects = value
                        ? DragDropEffects.Move | DragDropEffects.Copy
                        : DragDropEffects.Move;
                }
            }
        }

        internal int NextId()
        {
            _nextId++;
            return _nextId;
        }

        private static IEnumerable<ChangeItem> CreateItems()
        {
            var topics = new[]
            {
                "Design review",
                "API surface",
                "Performance sweep",
                "Docs polish",
                "Accessibility",
                "Test debt",
                "Theme tweaks",
                "Regression triage",
                "Toolkit sync",
                "Release checklist",
                "Localization",
                "UX polish",
                "Animations",
                "Instrumentation",
                "Crash triage",
                "Memory sweep",
                "Networking",
                "Caching",
                "Shadows",
                "Typography",
                "Forms overhaul",
                "Grid layout",
                "Data sync",
                "Theme docs",
                "Samples refresh",
                "Benchmark run",
                "QA signoff",
                "Release notes"
            };

            var value = 10;
            foreach (var topic in topics)
            {
                yield return new ChangeItem
                {
                    Id = value,
                    Name = topic,
                    Value = value
                };

                value += 10;
            }
        }

        private sealed class ChangeItemDropHandler : IDataGridRowDropHandler
        {
            private readonly RowDragDropViewModel _owner;
            private readonly DataGridRowReorderHandler _reorder = new();

            public ChangeItemDropHandler(RowDragDropViewModel owner)
            {
                _owner = owner;
            }

            public bool Validate(DataGridRowDropEventArgs args)
            {
                return _reorder.Validate(args);
            }

            public bool Execute(DataGridRowDropEventArgs args)
            {
                if (args.RequestedEffect == DragDropEffects.Copy && args.TargetList is IList list)
                {
                    var insertIndex = args.InsertIndex;
                    foreach (var item in args.Items.OfType<ChangeItem>())
                    {
                        var copy = new ChangeItem
                        {
                            Id = _owner.NextId(),
                            Name = $"{item.Name} (copy)",
                            Value = item.Value
                        };

                        list.Insert(insertIndex++, copy);
                    }

                    return true;
                }

                return _reorder.Execute(args);
            }
        }
    }
}
