using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.DataGridFiltering;
using Avalonia.Controls.DataGridHierarchical;
using DataGridSample.Behaviors;
using DataGridSample.Helpers;
using DataGridSample.Models;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace DataGridSample.ViewModels;

public sealed class HierarchyFeatureContractsViewModel : ReactiveObject
{
    public sealed class TreeItem : ReactiveObject
    {
        private string _name;
        private bool _isExpanded;

        public TreeItem(string name, string status, bool isRestricted = false, bool isExpanded = false)
        {
            _name = name;
            Status = status;
            IsRestricted = isRestricted;
            _isExpanded = isExpanded;
            Children = new ObservableCollection<TreeItem>();
        }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string Status { get; }

        public bool IsRestricted { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => this.RaiseAndSetIfChanged(ref _isExpanded, value);
        }

        public ObservableCollection<TreeItem> Children { get; }

        public override string ToString() => Name;
    }

    private const int MaximumEventCount = 36;

    private readonly DataGridColumnDefinition _nameColumn;
    private readonly TreeItem _programmaticRenameTarget;
    private string _filterText = string.Empty;
    private bool _blockRestrictedSelection = true;
    private TreeItem? _selectedItem;
    private int _eventSequence;
    private bool _renameToggle;

    public HierarchyFeatureContractsViewModel()
    {
        RootItems = CreateItems();
        _programmaticRenameTarget = RootItems[0].Children[0];

        Model = new HierarchicalModel<TreeItem>(new HierarchicalOptions<TreeItem>
        {
            ChildrenSelector = static item => item.Children,
            IsExpandedSelector = static item => item.IsExpanded,
            IsExpandedSetter = static (item, value) => item.IsExpanded = value,
            VirtualizeChildren = false,
        });
        Model.SetRoots(RootItems);

        _nameColumn = new DataGridHierarchicalColumnDefinition
        {
            Header = "Name (edit to commit)",
            Binding = CreateNodeBinding(
                "Name",
                static item => item.Name,
                static (item, value) => item.Name = value ?? string.Empty),
            ValueAccessor = new DataGridColumnValueAccessor<TreeItem, string>(
                static item => item.Name,
                static (item, value) => item.Name = value ?? string.Empty),
            IsReadOnly = false,
            DisplayMode = DataGridColumnDisplayMode.Retained,
            UseDirectCell = false,
            Width = new DataGridLength(2, DataGridLengthUnitType.Star),
        };

        var statusColumn = new DataGridTextColumnDefinition
        {
            Header = "Status",
            Binding = CreateNodeBinding("Status", static item => item.Status),
            ValueAccessor = new DataGridColumnValueAccessor<TreeItem, string>(static item => item.Status),
            IsReadOnly = true,
            DisplayMode = DataGridColumnDisplayMode.Retained,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star),
        };

        ColumnDefinitions = new ObservableCollection<DataGridColumnDefinition>
        {
            _nameColumn,
            statusColumn,
        };

        FilteringModel = new FilteringModel();
        Events = new ObservableCollection<string>();

        ApplyFilterCommand = ReactiveCommand.Create(ApplyFilter);
        ClearFilterCommand = ReactiveCommand.Create(ClearFilter);
        ExpandAllCommand = ReactiveCommand.Create(() => Model.ExpandAll());
        CollapseAllCommand = ReactiveCommand.Create(() => Model.CollapseAll());
        ProgrammaticRenameCommand = ReactiveCommand.Create(RenameProgrammatically);
        ClearEventsCommand = ReactiveCommand.Create(Events.Clear);
        FeatureEventCommand = ReactiveCommand.Create<DataGridFeatureContractEvent>(HandleFeatureEvent);
    }

    public ObservableCollection<TreeItem> RootItems { get; }

    public HierarchicalModel<TreeItem> Model { get; }

    public ObservableCollection<DataGridColumnDefinition> ColumnDefinitions { get; }

    public FilteringModel FilteringModel { get; }

    public ObservableCollection<string> Events { get; }

    public ReactiveCommand<RxVoid, RxVoid> ApplyFilterCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ClearFilterCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ExpandAllCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> CollapseAllCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ProgrammaticRenameCommand { get; }

    public ReactiveCommand<RxVoid, RxVoid> ClearEventsCommand { get; }

    public ReactiveCommand<DataGridFeatureContractEvent, RxVoid> FeatureEventCommand { get; }

    public string FilterText
    {
        get => _filterText;
        set => this.RaiseAndSetIfChanged(ref _filterText, value);
    }

    public bool BlockRestrictedSelection
    {
        get => _blockRestrictedSelection;
        set => this.RaiseAndSetIfChanged(ref _blockRestrictedSelection, value);
    }

    public TreeItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (!ReferenceEquals(_selectedItem, value))
            {
                this.RaiseAndSetIfChanged(ref _selectedItem, value);
                this.RaisePropertyChanged(nameof(SelectedItemSummary));
            }
        }
    }

    public string SelectedItemSummary =>
        SelectedItem is null ? "No selected item" : $"Selected: {SelectedItem.Name}";

    private static DataGridBindingDefinition CreateNodeBinding(
        string name,
        Func<TreeItem, string> getter,
        Action<TreeItem, string>? setter = null)
    {
        return ColumnDefinitionBindingFactory.CreateBinding<HierarchicalNode, string>(
            name,
            node => getter((TreeItem)node.Item),
            setter is null
                ? null
                : (node, value) => setter((TreeItem)node.Item, value));
    }

    private void ApplyFilter()
    {
        string text = FilterText.Trim();
        if (text.Length == 0)
        {
            FilteringModel.Remove(_nameColumn);
            return;
        }

        FilteringModel.SetOrUpdate(new FilteringDescriptor(
            _nameColumn,
            FilteringOperator.Contains,
            value: text));
    }

    private void ClearFilter()
    {
        FilterText = string.Empty;
        FilteringModel.Remove(_nameColumn);
    }

    private void RenameProgrammatically()
    {
        _renameToggle = !_renameToggle;
        _programmaticRenameTarget.Name = _renameToggle ? "API (model update)" : "API";
    }

    private void HandleFeatureEvent(DataGridFeatureContractEvent message)
    {
        bool veto = message.Kind == DataGridFeatureContractEventKind.SelectionChanging &&
            BlockRestrictedSelection &&
            message.AddedItems.OfType<TreeItem>().Any(static item => item.IsRestricted);
        message.Cancel = veto;

        string prefix = veto ? "VETO" : message.Kind.ToString();
        Events.Insert(0, $"{++_eventSequence:000} {prefix}: {message.Message}");
        while (Events.Count > MaximumEventCount)
        {
            Events.RemoveAt(Events.Count - 1);
        }
    }

    private static ObservableCollection<TreeItem> CreateItems()
    {
        var platform = new TreeItem("Platform", "Program", isExpanded: true);
        platform.Children.Add(new TreeItem("API", "Active"));
        platform.Children.Add(new TreeItem("Automation", "Active"));
        platform.Children.Add(new TreeItem("Archived prototype", "Restricted", isRestricted: true));

        var experience = new TreeItem("Experience", "Program", isExpanded: true);
        experience.Children.Add(new TreeItem("Desktop", "Active"));
        experience.Children.Add(new TreeItem("Accessibility", "Active"));
        experience.Children.Add(new TreeItem("Validation", "Review"));

        var delivery = new TreeItem("Delivery", "Program", isExpanded: true);
        for (int index = 1; index <= 18; index++)
        {
            delivery.Children.Add(new TreeItem($"Milestone {index:00}", index % 3 == 0 ? "Review" : "Active"));
        }

        return new ObservableCollection<TreeItem> { platform, experience, delivery };
    }
}

public sealed class HierarchyFeatureContractsViewModelFactory : IDataContextFactory
{
    public object Create() => new HierarchyFeatureContractsViewModel();
}
