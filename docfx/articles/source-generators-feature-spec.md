# ProDataGrid Source-Generation Expansion Specification

Status: approved implementation specification; core implementation available, advanced integrations in progress

Target: reflection-free source generation for complex reactive, streaming, remote, hierarchical, spreadsheet, and analytics applications

Last updated: 2026-08-08

Implementation checkpoint (2026-08-08): the canonical manifest and typed field API, direct attribute-scoped incremental schema, ViewModel, controller, indexed-column, and generated-view pipelines, assembly registry with optional Microsoft DI registration and explicit reflection-free XAML view mappings, stable item index, typed operation builders, named operation controller, controller factory/options customization, DynamicData `SourceList`/`SourceCache` ownership, bounded async/channel streaming, keyed snapshot reconciliation, remote queries, hierarchy loading and wrapper-aware compiled column bindings, keyed selection/state, grouping/summaries, editing/validation/undo, clipboard/fill, conditional rules, drag/drop, bands/chooser/layout, indexed columns, recycling templates, distinct values, performance profiles, pivot/chart/formula/outline metadata, localized providers, diagnostics, and Avalonia/ReactiveUI view recipes are implemented with focused tests. ProDiagnostics and its streaming viewer now serve as validation applications with eight generated schemas and no inline DataGrid columns or disabled compiled-binding scopes. Generated views also have Avalonia Headless coverage and deterministic screenshot verification. Assembly/namespace policy and registry discovery intentionally remain in the compilation-wide coordination lane; eliminating the residual scan when no such policy is present and the advanced items explicitly marked partial below remain tracked work.

## 1. Purpose

This specification defines the next source-generation capabilities for ProDataGrid. It is based on a static audit of the repository's sample applications, current public DataGrid feature surface, current generator implementation, generator tests, and documentation.

The proposal focuses on three outcomes:

1. Keep row access, data operations, state identity, templates, and view wiring reflection-free and NativeAOT-friendly.
2. Remove repeated sorting, filtering, searching, hierarchy, selection, streaming, and lifecycle code from complex reactive applications.
3. Preserve full customization through small runtime contracts, validated implementation types, partial hooks, and custom generated-view base classes.

Code blocks labelled **Proposed API** describe the remaining target shape. Unlabelled APIs are implemented unless their feature row below is marked partial.

### 1.1 Implementation coverage

| Feature | Status | Implemented boundary / remaining work |
|---|---|---|
| F01 incremental foundation | Core implemented | Direct type schemas, ViewModels, controllers, indexed-column triggers, and generated views use equatable attributed candidates with isolated composition and stable semantic/output reuse. View framework/collision facts and owner-driven schema options are part of the candidate graph. Assembly/namespace policy and registry coordination remain compilation-wide; the residual empty-policy scan is the remaining optimization. |
| F02–F07 identity and data operations | Implemented | Typed fields/builders, key/index services, operation ownership, DynamicData list/cache pipelines, bounded streams, snapshot reconciliation, and revisioned remote queries are available. |
| F08 hierarchy | Core implemented | Typed hierarchy delegates, async loading, expansion/key operations, reset preservation, and `HierarchicalRows` wrapper-aware compiled bindings are available; broader conversion of legacy sample trees remains. |
| F09–F14 data workflows | Implemented | Grouping, summaries, selection, versioned state/migration, editing/validation/undo, clipboard/fill/export, and conditional rules share canonical accessors. |
| F15 layout/indexed columns | Implemented | Nested band trees, chooser visibility/order/reset, layout state, method-backed indexed column families, and replaceable pin/freeze command bridges are available. |
| F16 templates/drawing | Partial | Typed recycling cell/edit/new-row templates are available; row-details and custom-drawing cache generation remain. |
| F17 drag/drop | Implemented | Keyed request/result adapters and domain-owned handlers are available. |
| F18 analytics | Core implemented | Typed pivot fields, neutral chart/outline/formula roles, compile-time formula dependency validation, and an optional reflection-free chart adapter are available; optional formula-parser analyzers and range projection remain. |
| F19 generated views | Core implemented | Avalonia and ReactiveUI code-only views, compiled binding indexers, custom bases, recipes, named slots, automation metadata, state bridges, and `[DataGridViewRegistration]` mappings for existing XAML views are available; richer command/event bridges and loading/error/empty projections remain. |
| F20 localization/accessibility/diagnostics | Implemented | Validated direct localization providers, resource keys, stable automation IDs/names/help, and generated diagnostics manifests are available. |
| F21 collection views/dynamic shapes | Partial | Typed collection-view factories and range-aware generated services are available; unknown runtime shapes still require explicit user adapters. |
| F22 header filtering/distinct values | Implemented | Typed editor metadata, bounded local/remote distinct-value providers, and cached per-field commands for sort/filter/visibility/pin/freeze/autosize/reset are available through a replaceable interaction boundary. |
| F23 performance/input diagnostics | Partial | Explicit performance profiles and generated runtime diagnostics are available; generated keyboard maps and renderer metric bridges remain. |

## 2. Existing baseline

The current generator described in [Column Definitions (Source Generators)](column-definitions-source-generators.md) already provides:

- `[GenerateDataGridColumns]` on item types and equivalent assembly/namespace discovery.
- `[DataGridColumn]` and `[DataGridIgnoreColumn]` metadata.
- All current generated column kinds.
- Stable keys, ordering, sizing, theme/resource keys, common column options, custom implementation types, factories, and configure hooks.
- Generated `DataGridColumnDefinition` collections and `DataGridFastPathOptions`.
- Typed getters/setters and compiled sort, filter, and search factories.
- DynamicData upstream-operation bypass support.
- `[GenerateDataGridViewModel]` augmentation.
- `[GenerateDataGridView]` code-only Avalonia and ReactiveUI views with custom bases and compiled binding indexers.
- Assembly/namespace-wide generation and diagnostics `PDGSG001` through `PDGSG014`.

The new work must extend these contracts additively. It must not create a second incompatible column metadata system.

## 3. Repository audit

### 3.1 Audited corpus

The audit covered all three application samples:

- `DataGridSample`: 189 page views plus application/window XAML, 169 C# files under `ViewModels`, and 41 model files.
- `ProDataGrid.ExcelSample`: workbook, spreadsheet, formula, fill, clipboard, selection, chart, ribbon, and sheet-tab scenarios.
- `ProDataGrid.MarketDashboardSample`: a live multi-grid/multi-chart ReactiveUI dashboard, async data service, snapshot reconciliation, commands, and DI composition.

Across these applications the audited surface contains:

- 199 `.axaml` files.
- 196 `.axaml.cs` files.
- 180 C# files under `ViewModel`/`ViewModels` folders.
- 47 C# files under `Model`/`Models` folders.
- 29,960 lines in `DataGridSample/ViewModels` alone.
- 15 handwritten DataGrid adapter/factory classes in `DataGridSample/Adapters`.

The audit also covered the 532 public types indexed in the current API documentation across collections, columns, sorting, filtering, searching, grouping, summaries, selection, hierarchy, clipboard, filling, editing, conditional formatting, drag/drop, state, pivoting, reporting, formula, charting, sizing, and diagnostics namespaces.

### 3.2 Quantitative findings

The main sample application contains:

- 170 page views with `ItemsSource` bindings.
- 75 page views using `ColumnDefinitionsSource`.
- 844 explicit DataGrid column elements, including 494 text, 142 template, 100 numeric, 52 hierarchical, and 15 custom-drawing columns.
- 26 hierarchical/tree pages with `x:CompileBindings="False"` even though the application defaults to compiled bindings.
- 177 page code-behind files attaching `DataContext` from `AttachedToVisualTree`.
- 42 page code-behind files with 157 custom event-handler methods.
- 68 XAML `Click="On..."` handlers, plus sorting, selection, editing, lifecycle, clipboard, scroll, and column handlers.
- 26 ViewModel/adapter files constructing 73 sorting, filtering, or search descriptors.
- 27 ViewModel/adapter files manually subscribing to sorting, filtering, search, or selection change events.
- 21 ViewModels using `DeferRefresh` manually.
- 8 DynamicData ViewModels with 15 `SourceList`/`SourceCache` instances and 28 `BehaviorSubject` instances.
- 13 files with 77 handwritten `nameof(...) => ...` property-path switch arms.
- 12 ViewModels directly importing `Avalonia.Threading`.

These counts are not quality metrics. They identify repeated integration seams that source generation can safely standardize.

### 3.3 Sample-derived feature matrix

The page-family counts overlap because several pages exercise more than one feature.

| Feature family | Audited page families | Generator opportunity |
|---|---:|---|
| Generated/auto/bindable/dynamic columns | 36 | Extend schemas to bands, indexed column families, templates, localization, and registries. |
| Sort/filter/search/group | 33 | Generate typed descriptor builders, presets, models, ownership, and operation controllers. |
| Selection/navigation | 28 | Generate stable keys, fast index resolvers, shared selection, and state restoration. |
| Hierarchy/tree | 26 | Generate `HierarchicalOptions<T>`, expansion/key accessors, typed node bindings, and streaming adapters. |
| Dynamic/live/range updates | 15 | Generate DynamicData and async-stream pipelines, batching, scheduling, and disposal. |
| Pivot analytics | 14 | Generate typed axis/value/calculated-field selectors and layout profiles. |
| Formula/Power Fx | 8 | Generate formula metadata, dependency/value access, validation, and static rule registration. |
| Summaries | 5 | Generate typed summary descriptions and incremental aggregate accessors. |
| State persistence | 10 | Generate stable key maps, schema versions, serializers, and migration hooks. |
| Clipboard/fill/edit/validation | 12 | Generate typed conversion, import/export, fill, validation, and edit-policy adapters. |
| Drag/drop | 8 | Generate keyed flat/hierarchical reorder adapters and command/interaction bridges. |
| Chart/report integration | 4 | Generate typed chart series, range projections, selection synchronization, and outline fields. |
| Virtualization/scroll/performance | 14 | Generate performance profiles, row-height/key accessors, and runtime diagnostics manifests. |
| Styling/conditional formatting | 4 | Generate typed predicates and resource-key metadata while leaving visuals in resources. |
| Complex mimic/application layouts | 16 | Add reusable view recipes without attempting to generate bespoke shells. |
| Frozen/layout/banding | 5 | Generate band trees, visibility/chooser metadata, width groups, and layout-state keys. |

Representative evidence includes:

- `GeneratedColumnsDynamicDataViewModel` demonstrates the current best path but still manually owns three models, three subjects, three model-event subscriptions, the DynamicData pipeline, commands, and disposal.
- The DynamicData adapter folder repeats property-path switches and descriptor translation for flat and hierarchical sources.
- Hierarchical pages disable compiled XAML binding because the runtime node wrapper is difficult to type in XAML.
- `SheetViewModel` builds runtime indexed columns with handwritten `IPropertyInfo`, typed delegates, formula special cases, and per-slot configuration.
- The Excel sample requires handwritten attached binders for fast-path options, grid selection state, clipboard state, and row-drag policy.
- `StateFullPage` performs capture, migration-sensitive key resolution, serialization, and restore directly in the view.
- The market dashboard reconciles service snapshots into multiple collections, dispatches updates to the UI scheduler, coordinates five grids/charts, and manually owns many commands and event subscriptions.

### 3.4 What should not be generated

The samples also contain logic that must remain user code:

- Domain rules, order execution, portfolio accounting, and external API clients.
- Bespoke application shells such as the complete Excel ribbon or market terminal layout.
- Arbitrary formula or Power Fx evaluation semantics.
- Branding, resource dictionaries, control themes, and visual design.
- Custom chart rendering algorithms.
- Data storage, networking, authentication, and retry policy.

The generator should produce typed metadata, adapters, controllers, and optional reusable view composition. It should not become an application framework.

### 3.5 ProDiagnostics validation migration

ProDiagnostics is a production validation lane, not a synthetic sample. It combines flat read-only grids, editable template cells, multi-grid view models, live telemetry, column visibility, hierarchical wrappers, existing XAML views, and an inspector whose domain is arbitrary runtime objects.

| Surface | Row type | Generated contract | Validation purpose |
|---|---|---|---|
| Viewer metrics | `MetricSeriesViewModel` | streaming schema, keyed template, fast-path options, layout controller | high-frequency updates, numeric formatting, reusable trend cell, column chooser |
| Viewer activities | `ActivityEventViewModel` | streaming schema and second named ViewModel projection | multiple schemas on one ViewModel and formatted telemetry rows |
| Assets | `AssetEntryViewModel` | attributed-only schema | sortable reflection-free read-only grid |
| Control properties | `PropertyViewModel` | shared template schema and generated layout visibility | editable recycling template plus runtime column-profile switching |
| Resource details | `PropertyViewModel` | second ViewModel projection of the shared schema | schema reuse with per-view layout policy |
| Resource picker | `ResourceReferenceEntryViewModel` | text/template schema | external collection-view sort/filter ownership |
| Resources | `ResourceEntryViewModel` and `ResourceTreeNode` | two named schemas on one ViewModel | flat and hierarchical grids on the same screen |
| Visual/logical tree | `TreeNode` | hierarchical-row schema | compiled binding through `HierarchicalNode.Item` with no XAML binding fallback |

The migration introduced two APIs because the application exposed real gaps:

```csharp
[GenerateDataGridColumns(
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    HierarchicalRows = true)]
internal abstract class TreeNode
{
    [DataGridColumn(
        DataGridColumnKind.Hierarchical,
        SortMemberPath = nameof(Type),
        TemplateKey = "VisualTreeNodeCellTemplate")]
    public TreeNode Item => this;
}
```

`HierarchicalRows` keeps canonical schema accessors typed to `TItem`, but emits separate compiled column bindings and value accessors typed to `HierarchicalNode`. Generated sort paths are prefixed with `Item.`. This preserves typed data operations while matching the row wrapper actually presented by a hierarchical DataGrid.

```csharp
[assembly: GenerateDataGridRegistry(
    RegistryName = "ProDiagnosticsGeneratedSchemas",
    RegistryNamespace = "Avalonia.Diagnostics.Generated")]
[assembly: DataGridViewRegistration(typeof(TreePageViewModel), typeof(TreePageView))]

// Generated registry usage; no Type.GetType or Activator.CreateInstance.
if (ProDiagnosticsGeneratedSchemas.TryCreateView(viewModel, out Control? view))
{
    return view;
}
```

Multiple `[GenerateDataGridViewModel]` attributes on one partial type are supported when each projection has distinct member names. Hint names include the column-definition member name so the generated files remain deterministic and collision-free.

The reflection-free boundary is explicit:

- ProDiagnostics' own grid schemas, compiled column bindings, fast-path options, column layout, schema registry, and view lookup are generated.
- Reflection used only to inspect unknown third-party runtime objects remains in the inspector domain. It is not a DataGrid binding or view-location fallback.
- Future inspected assemblies may opt into generated inspection metadata. Unknown types still require the inspector's dynamic provider; silently pretending they are statically knowable would make the diagnostics tool incomplete.

Migration validation includes generator-driver tests for hierarchical wrappers, multi-grid ViewModels, registered XAML views, generated-code compilation, ProDiagnostics registry/schema tests, Avalonia Headless view creation, and the complete ProDiagnostics test suite. A repository audit test or build check should continue to prevent reintroduction of inline DataGrid columns and `x:CompileBindings="False"` in these two validation projects.

## 4. Design principles

### 4.1 Reflection-free by construction

Generated paths must not call `Type.GetProperty`, `PropertyDescriptor`, expression compilation, `DynamicInvoke`, or runtime XAML loading to discover row members. A relaxed compatibility mode may use existing DataGrid behavior outside a generated controller, but generated strict mode must report an error or disable the affected feature instead of silently adding reflection.

### 4.2 One canonical schema

Column keys, property keys, typed accessors, summaries, conditional rules, state, hierarchy, export, chart, and pivot metadata must reference one generated schema manifest. The same property must not be rediscovered independently by each feature.

### 4.3 Explicit operation ownership

Sorting, filtering, and searching each have exactly one execution owner:

- `View`: `DataGridCollectionView` applies the descriptors.
- `ExternalPipeline`: DynamicData or another local reactive pipeline applies them.
- `Remote`: a query provider applies them server-side.

Generated adapters must set the existing ownership flags consistently. A generated application must never sort or filter twice.

### 4.4 Strict MVVM layering

New generated ViewModel/controller APIs should be UI-framework neutral. The preferred architecture is:

1. A generated metadata and operation controller in a small presentation runtime.
2. A generated Avalonia adapter that maps the controller to existing DataGrid models and events.
3. An optional generated view that owns only UI composition and adapter activation.

The existing `[GenerateDataGridViewModel]` members that expose Avalonia DataGrid model types remain supported for compatibility. New complex-application APIs should prefer the layered controller mode.

### 4.5 Framework strategy, not framework lock-in

The core output must work with plain INPC. ReactiveUI adds activation, `ReactiveCommand`, schedulers, observable properties, and `Interaction<TInput,TOutput>` where relevant. Additional MVVM strategies may be added later as separate generator strategies without changing schema metadata.

### 4.6 Bounded lifetime and backpressure

Every generated subscription, timer, channel, and cache has an explicit owner and a deterministic disposal path. Async and streaming adapters require an explicit buffer/coalescing policy. Unbounded queues are not a default.

### 4.7 Customization precedence

For every extensible feature, precedence is:

1. Explicit user implementation/factory type.
2. A correctly shaped named partial hook or factory method.
3. Generated default implementation.
4. Existing runtime fallback only when strict mode is disabled and the user has explicitly selected fallback behavior.

Property metadata overrides type defaults; type defaults override namespace defaults; namespace defaults override assembly defaults.

## 5. Proposed architecture

### 5.1 Packages and layers

| Layer | Responsibility | Dependencies |
|---|---|---|
| `ProDataGrid.SourceGeneration.Abstractions` | Public attributes/enums needed across assemblies. | BCL only. |
| `ProDataGrid.SourceGenerators` | Incremental discovery, validation, and emission. | Roslyn only. |
| `ProDataGrid.Generation.Runtime` | UI-neutral schema, query, controller, lifetime, and state contracts. | BCL; optional System.Reactive abstractions only if unavoidable. |
| `ProDataGrid.Generation.Avalonia` | Adapters to DataGrid models, bindings, interactions, and generated views. | Avalonia and ProDataGrid. |
| `ProDataGrid.Generation.DynamicData` | `SourceList`/`SourceCache` pipelines and change-set policies. | DynamicData. |
| `ProDataGrid.Generation.ReactiveUI` | Activation, commands, interactions, and schedulers. | ReactiveUI, not `Avalonia.ReactiveUI`. |

Package splitting is a target architecture. Initial implementation may keep assemblies consolidated while enforcing these dependency boundaries internally.

### 5.2 Incremental generator pipeline

The current generator builds its full model from `CompilationProvider`. Before adding broad feature discovery, it should move to isolated incremental pipelines:

- `ForAttributeWithMetadataName` for item, property, ViewModel, view, assembly, and namespace triggers.
- Small immutable, equatable semantic models before `.Collect()`.
- Separate pipelines for schema, controllers, views, registries, and diagnostics.
- Reference/capability detection isolated from source syntax changes.
- Assembly/namespace expansion collected only within the affected policy scope.
- Stable hint names based on metadata identity and feature name.
- Deterministic ordering independent of syntax tree order.
- Cancellation checks in discovery and emission loops.

Editing one row type must not invalidate generated outputs for unrelated schemas or views.

### 5.3 Generated manifest

Every schema should expose a versioned manifest that is reusable by all generated features.

```csharp
// Proposed generated shape; abbreviated.
public static class TradeGridSchema
{
    public const int ManifestVersion = 1;
    public const string SchemaId = "Trading.Trade/v1";
    public const string SchemaHash = "...";

    public static ReadOnlySpan<DataGridGeneratedField> Fields { get; }
    public static DataGridGeneratedAccessor<Trade, int> Id { get; }
    public static DataGridGeneratedAccessor<Trade, decimal> Price { get; }

    public static bool TryGetField(string key, out DataGridGeneratedField field);
    public static IComparer<Trade> CreateSortComparer(ReadOnlySpan<GridSort> sorts);
    public static Predicate<Trade> CreateFilter(ReadOnlySpan<GridFilter> filters);
    public static Predicate<Trade> CreateSearch(in GridSearch search);
}
```

The runtime manifest stores stable IDs and delegates, not reflection metadata. A diagnostic/debug view may expose names and types, but hot paths use ordinal field IDs or generated switch dispatch.

### 5.4 Reflection-free registry and DI

For each assembly the generator should optionally emit:

```csharp
public static class GeneratedProDataGridRegistration
{
    public static IServiceCollection AddGeneratedProDataGrids(
        this IServiceCollection services);

    public static bool TryGetSchema(
        Type itemType,
        out IDataGridGeneratedSchema schema);

    public static bool TryCreateView(
        Type viewModelType,
        out Control view);
}
```

The implementation is a generated type switch or frozen lookup table. It must not scan assemblies. A non-DI overload should remain available so Microsoft DI is optional.
The `IServiceCollection` overload is emitted only when Microsoft DI is referenced.

## 6. Proposed API conventions

### 6.1 Controller trigger

```csharp
[Flags]
public enum DataGridGeneratedFeatures
{
    None = 0,
    Columns = 1 << 0,
    Sorting = 1 << 1,
    Filtering = 1 << 2,
    Searching = 1 << 3,
    Selection = 1 << 4,
    State = 1 << 5,
    Hierarchy = 1 << 6,
    Grouping = 1 << 7,
    Summaries = 1 << 8,
    ConditionalFormatting = 1 << 9,
    Editing = 1 << 10,
    Clipboard = 1 << 11,
    Fill = 1 << 12,
    DragDrop = 1 << 13,
    Diagnostics = 1 << 14
}

public enum DataGridGeneratedSourceKind
{
    Enumerable,
    ObservableCollection,
    DynamicDataSourceList,
    DynamicDataSourceCache,
    AsyncEnumerable,
    ChannelReader,
    Remote
}

public enum DataGridOperationExecution
{
    View,
    ExternalPipeline,
    Remote
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class GenerateDataGridControllerAttribute : Attribute
{
    public GenerateDataGridControllerAttribute(Type itemType, string name);

    public string? ProviderName { get; set; }
    public string? SourceMember { get; set; }
    public DataGridGeneratedSourceKind SourceKind { get; set; }
    public DataGridGeneratedFeatures Features { get; set; }
    public DataGridOperationExecution OperationExecution { get; set; }
    public string? KeyMember { get; set; }
    public Type? ImplementationType { get; set; }
    public string? ConfigureMethod { get; set; }
    public bool Strict { get; set; } = true;
    public bool Streaming { get; set; }
}
```

The `name` is required so one ViewModel may own several grids without member collisions. Existing `[GenerateDataGridViewModel]` remains the concise single-grid compatibility API.

### 6.2 Item metadata

The existing `[DataGridColumn]` remains the primary property attribute. The following focused attributes extend its schema:

```csharp
[DataGridKey]
[DataGridChildren]
[DataGridExpanded]
[DataGridParentKey]
[DataGridGroup(Order = 0, Direction = ListSortDirection.Ascending)]
[DataGridSummary(DataGridAggregateType.Sum, Scope = DataGridSummaryScope.Both)]
[DataGridConditionalFormat(
    DataGridCondition.GreaterThan,
    Operand = "1000000",
    CellThemeKey = "LargeTradeCell")]
[DataGridBand("Execution", Order = 1)]
[DataGridExport(Format = "N2", NullText = "-")]
[DataGridValidation(ValidatorMethod = nameof(ValidatePrice))]
[DataGridPivotAxis(DataGridPivotFieldArea.Rows, Order = 0)]
[DataGridPivotValue(PivotAggregateType.Sum, Format = "C2")]
[DataGridChartValue(Series = "Price", Role = DataGridChartRole.Value)]
```

Attributes that reference user code use `nameof(...)`. Discovery validates the complete method signature and accessibility.

### 6.3 Small customization contracts

Do not introduce one feature-factory interface with dozens of methods. Use focused contracts such as:

```csharp
public interface IDataGridItemKey<TItem, TKey>
{
    TKey GetKey(TItem item);
}

public interface IDataGridGeneratedFilterFactory<TItem>
{
    Predicate<TItem> Create(ReadOnlySpan<GridFilter> filters);
}

public interface IDataGridGeneratedSummaryFactory<TItem>
{
    IDataGridIncrementalSummary<TItem> Create(in DataGridSummaryContext context);
}

public interface IDataGridGeneratedViewAdapter<TViewModel>
{
    void Attach(Control view, TViewModel viewModel, CompositeDisposable lifetime);
}
```

An implementation type is emitted as a direct constructor or static factory call after compile-time validation. The generator never instantiates user code inside the compiler process.

### 6.4 Generated output names

For a controller named `Trades`, the default generated members are grouped under one property:

```csharp
public TradeGridController Trades { get; private set; } = null!;
public void InitializeTrades(TradeGridController controller);
```

The controller exposes `Items`, `Columns`, `FastPath`, operation state, selection, state commands, diagnostics, and lifetime. Explicit initialization is the initial lifetime model: the user constructs the source and generated controller in the constructor, passes it to the generated initialization method, and disposes it with the ViewModel. Flat forwarding properties are opt-in for compatibility with existing XAML. This prevents a ViewModel from receiving twenty generated top-level members per grid.

## 7. Feature specifications

### F01. Incremental foundation and compatibility manifest — P0

Requirements:

- Refactor discovery away from one full-compilation transform.
- Emit one canonical manifest per schema.
- Emit deterministic cross-assembly registries when requested.
- Preserve all existing generated type/member names unless the user selects the new controller API.
- Add a schema format version and stable schema ID.
- Test incremental step caching and deterministic output.

This work is a prerequisite for every other feature.

### F02. Stable identity and fast index resolution — P0

The selection, state, DynamicData `SourceCache`, hierarchy, drag/drop, and chart coordination samples all need stable identity.

Requirements:

- `[DataGridKey]` on a field/property or `KeyMember` on the controller.
- Composite keys through a validated static method.
- Generated typed key selector, equality comparer, key-to-item resolver, and optional item-to-index cache.
- Incremental cache updates for add/remove/move/replace/reset.
- Reference-identity mode only when explicitly selected.
- Diagnostics for nullable, mutable, non-unique, missing, or incompatible keys.
- Reuse the same key in `DataGridStateOptions`, selection preservation, DynamicData cache integration, drag/drop, and chart selection.

The generator should use value-type generic key paths where possible and avoid boxing keys on the steady-state selection path. Existing object-key APIs may be bridged at the Avalonia adapter boundary.

### F03. Typed operation descriptors, builders, and presets — P0

Manual string paths and repeated descriptor construction should be replaced with generated field references:

```csharp
Trades.Sort.Set(
    TradeGridSchema.Price.Descending(),
    TradeGridSchema.Timestamp.Descending());

Trades.Filter.Set(
    TradeGridSchema.Desk.Contains(DeskFilter, StringComparison.OrdinalIgnoreCase),
    TradeGridSchema.Price.GreaterThanOrEqual(MinimumPrice));

Trades.Search.Set(GridSearch.Contains(Query));
```

Requirements:

- Strongly typed builders for every supported filtering operator.
- Compile-time operator/type validation.
- Stable field IDs in normalized descriptors.
- Reusable named sort/filter/search presets declared by attributes or static methods.
- Allocation-conscious `Set`, `SetOrUpdate`, and deferred/batched updates.
- Conversion to existing `SortingDescriptor`, `FilteringDescriptor`, and `SearchDescriptor` only at the UI adapter boundary.
- Custom comparer/predicate hooks remain supported.

### F04. Generated operation controller — P1

Generate sorting, filtering, and search state plus the correct adapter ownership from one controller declaration.

Requirements:

- Independent enablement of sort, filter, and search.
- Multi-sort and sort-cycle configuration.
- Filter combination (`All`/`Any`) and per-field combination policy.
- Search scope, match mode, highlighting, navigation, and debounce configuration.
- Descriptor chips/summaries as optional projected read-only collections.
- Generated clear/apply-preset/remove-descriptor commands in the selected MVVM strategy.
- Correct `OwnsViewSorts`/`OwnsViewFilter` and search ownership behavior.
- No event subscription in the user's ViewModel for standard operation propagation.
- One controller can be headless-tested without creating a DataGrid.

ReactiveUI mode should use `ReactiveCommand` and activation-aware subscriptions. Plain mode should expose commands through small framework-neutral command interfaces or `ICommand` only in the UI adapter.

### F05. DynamicData pipelines — P1

This feature replaces the repeated `BehaviorSubject`, model-event, adapter-factory, `SortAndBind`, and disposal code in the DynamicData samples.

Requirements:

- `SourceList<T>` and `SourceCache<T,TKey>` support.
- Generated filter, search, and multi-sort pipelines using the canonical schema.
- `SortAndBind` configuration including `UseReplaceForUpdates`.
- Source-cache key reuse from `[DataGridKey]`.
- Flat and hierarchical change sets.
- External operation ownership so the DataGrid never re-applies operations.
- Optional transform stage with a user-supplied typed implementation.
- Optional grouping and incremental summaries after filtering.
- Explicit observe-on scheduler at the final UI collection boundary only.
- Test-scheduler support.
- One disposable controller owns all subjects and subscriptions.
- Error and completion observables; no swallowed pipeline failures.

The generated filter/search predicate should be rebuilt only when descriptor revisions change. Row evaluation must not allocate and must not box primitive property values when a typed operator exists.

### F06. Async streams, channels, and snapshot reconciliation — P1

The streaming pages and market dashboard require sources beyond DynamicData.

Requirements:

- `IAsyncEnumerable<T>` and `ChannelReader<T>` adapters.
- Append, upsert-by-key, remove-by-key, and replace-snapshot modes.
- Generated keyed snapshot diffing instead of unconditional collection clear/repopulate.
- Configurable batch size, time window, bounded capacity, and overflow policy.
- Cancellation on controller disposal or ReactiveUI deactivation, as configured.
- Background ingestion with exactly one UI scheduler hop per emitted batch.
- Monotonic revision IDs so stale snapshots or remote responses are ignored.
- Metrics for queued, coalesced, dropped, applied, and stale updates.

Default streaming policy should be bounded coalescing by key, not an unbounded queue.

### F07. Remote/server-side query controller — P1

Complex applications often cannot materialize all rows locally.

**Proposed API:**

```csharp
public interface IDataGridQueryProvider<TItem, TKey>
{
    ValueTask<DataGridQueryPage<TItem, TKey>> ExecuteAsync(
        DataGridQuery query,
        CancellationToken cancellationToken);
}

public sealed record DataGridQuery(
    long Revision,
    GridSortSet Sorts,
    GridFilterSet Filters,
    GridSearch Search,
    DataGridPageRequest Page,
    GridGroupSet Groups);
```

Requirements:

- Offset and cursor paging.
- Cancellation and stale-response suppression.
- Debounce/coalescing of rapid descriptor changes.
- Optional page cache and prefetch policy.
- Total count, unknown count, and streaming continuation support.
- Translation hooks from stable generated field IDs to backend field names.
- Provider errors exposed as state suitable for ReactiveUI binding/interactions.
- No network or persistence implementation in generated code.

### F08. Hierarchical schemas and typed node bindings — P1

**Proposed metadata:**

```csharp
public sealed class FolderNode
{
    [DataGridKey]
    public required Guid Id { get; init; }

    [DataGridChildren]
    public ObservableCollection<FolderNode> Children { get; } = [];

    [DataGridExpanded]
    public bool IsExpanded { get; set; }

    [DataGridColumn(DataGridColumnKind.Hierarchical, ColumnKey = "name")]
    public required string Name { get; init; }
}
```

Requirements:

- Generate `HierarchicalOptions<T>` delegates for children, leaf, expanded getter/setter, and identity.
- Optional parent-key/path and depth selectors.
- Async child-loader hook returning `ValueTask<IReadOnlyList<T>>`.
- Expand/collapse-all and expand-to-key operations.
- Preserve expansion and selection across resets and source swaps.
- Hierarchical filtering modes: ancestors-of-match, descendants-of-match, match-only, and custom.
- Sibling-only and global sort policies.
- SourceList/SourceCache hierarchy support.
- Cycle and duplicate-key diagnostics in strict/debug mode.
- Generate a public typed node projection or binding indexer so hierarchical views can keep compiled bindings enabled.

The final requirement directly addresses the 26 audited pages that currently opt out of compiled bindings.

### F09. Grouping, summaries, and incremental aggregates — P2

Requirements:

- Generate typed group selectors instead of path-based group descriptions.
- Generate default group order, comparer, key formatter, and expansion key.
- Generate total/group summary descriptions from `[DataGridSummary]`.
- Support count, distinct count, sum, average, min, max, and custom calculators.
- Reuse typed column accessors for summary value reads.
- Incremental add/remove/replace aggregation for streaming sources.
- Define reset fallback for calculators that cannot reverse a removed value.
- Preserve current `IDataGridSummaryCalculator` customization.
- Support summary scope, placement, string format, alignment, theme key, and title.

The DataGrid runtime may need a typed group-description contract and a summary value-accessor property so generated code can avoid existing path lookup.

### F10. Selection, current cell, and shared selection — P1

Requirements:

- Generate a typed `SelectionModel<T>` configuration from the canonical item key.
- Generate fast index cache or `IDataGridIndexOf` integration.
- Row, cell, column, and mixed selection-unit profiles.
- Strongly typed selected-items and current-item projections.
- Selection preservation across sort/filter/page/hierarchy/source reset.
- Shared selection between multiple grids or another selecting control.
- Selection-origin stream for pointer, keyboard, binding, model, and restore changes.
- Optional ReactiveUI commands for select-all, clear, select-by-key, and range selection.
- Chart/grid selection bridge based on item and column keys.

Selection state must be stored by key, not only by row index.

### F11. State persistence and migration — P1

Requirements:

- Generate `DataGridStateOptions` column/item key selectors and resolvers.
- Generate a stable schema ID, schema hash, and user-controlled state version.
- Capture/restore selected sections through a generated UI adapter or interaction, keeping DataGrid access out of the ViewModel.
- Generate JSON serialization metadata where the configured serializer supports source generation.
- Support partial migration hooks:

```csharp
static partial bool TryMigrateTradesState(
    int fromVersion,
    int toVersion,
    ref DataGridState state);
```

- Support renamed/removed/split/merged column-key maps.
- Restore columns, operations, conditional formatting, grouping, hierarchy, selection, and scroll independently.
- Report unstable keys and schema-breaking changes at compile time where possible.
- Never persist delegates, controls, templates, or arbitrary runtime objects.

ReactiveUI generated views should use an `Interaction` or generated view adapter to execute capture/restore against the actual DataGrid.

### F12. Editing, conversion, validation, and undo — P2

Requirements:

- Generate typed setters, null handling, culture-aware parsers, and formatters.
- Compile common `DataAnnotations` validation rules into direct code.
- Validate custom methods referenced with `nameof`.
- Generate per-column edit eligibility and coercion hooks.
- Generate editing interaction model/factory implementations for declared trigger profiles.
- Optional `INotifyDataErrorInfo` and ReactiveUI-compatible validation projections.
- Optional keyed edit transactions with before/after values for undo/redo services.
- Cross-field validation stays in user code through a small service/hook.
- Async validation must be cancellable, revisioned, and must not block the UI thread.

The generator should not create a general-purpose validation framework; it should adapt declared rules to existing DataGrid editing behavior.

### F13. Clipboard import/export and fill — P2

Requirements:

- Generate typed cell-to-text and text-to-cell converters per column.
- Support text, CSV, HTML, Markdown, XML, YAML, and JSON export metadata already exposed by DataGrid.
- Generate header/key maps independent of display order.
- Culture, null, quoting, formula, and error policies.
- Generate `IDataGridClipboardImportModel` and factory adapters for standard cases.
- Generate standard copy, numeric/date sequence, relative formula, and custom fill strategies.
- Rectangular selection and dynamic/indexed column-family support.
- Maximum cell count and payload size limits for import.
- Paste/fill validation should batch notifications and return structured errors.

This should replace most of the Excel sample's handwritten clipboard and fill plumbing while retaining its spreadsheet-specific policy as a custom implementation.

### F14. Conditional formatting and style metadata — P2

Requirements:

- Compile simple comparisons, range, null, text, and row predicates from metadata.
- Reuse typed field accessors and typed constant conversion.
- Cell/row theme keys, foreground/background binding accessors, order, scope, and stop-if-true.
- Named rules and runtime enable/disable state.
- Custom static predicate methods for complex rules.
- Optional Power Fx rule provider integration without embedding Power Fx in the core generator.
- Resource keys remain strings because resources are resolved by Avalonia; optional resource-manifest validation may warn when a key is known to be absent.

Generated predicates must not allocate per evaluated cell.

### F15. Column bands, chooser, layout, and indexed column families — P2

Requirements:

- Generate band trees from repeatable `[DataGridBand]` metadata.
- Default display order, visibility, resize/reorder/hide permissions, width-sharing groups, and left/right frozen placement.
- Generate column chooser items and commands keyed by schema column ID.
- Support fixed property columns and runtime indexed/method-backed column families.

**Proposed indexed-column API:**

```csharp
[GenerateDataGridIndexedColumns(
    Name = "Cells",
    GetterMethod = nameof(GetCell),
    SetterMethod = nameof(SetCell),
    NotificationNameMethod = nameof(GetCellPropertyName))]
public sealed partial class SpreadsheetRow
{
    public object? GetCell(int index) => ...;
    public void SetCell(int index, object? value) => ...;
    public static string GetCellPropertyName(int index) => ...;
}
```

The generated schema should expose:

```csharp
DataGridColumnDefinition CreateCellColumn<TValue>(
    int index,
    in DataGridIndexedColumnOptions<TValue> options);
```

This removes the duplicated handwritten `ClrPropertyInfo`/`DataGridBindingDefinition` helper pattern while keeping runtime column count and per-slot customization.

For `DataTable`, dictionaries, or other truly runtime-defined shapes, the generator cannot infer a schema. It should generate only an adapter shell around a user-supplied typed/dynamic accessor provider and clearly mark that path as runtime-defined.

### F16. Templates, row details, and custom drawing — P2

Requirements:

- Typed/recycling `FuncDataTemplate<T>` generation from validated factory methods.
- Resource-key templates remain supported.
- Row-details template and visibility-policy metadata.
- Nested-grid schema references without reflection view lookup.
- Custom drawing operation factory and invalidation-source hooks.
- Generated cell cache keys for `IDataGridCellDrawOperationItemCache` implementations.
- Button/toggle command and parameter accessors with compiled bindings.
- Accessibility metadata for generated template roots.

The generator must not serialize arbitrary control trees into attributes. Common generated-view recipes and user-authored resource templates are the supported composition mechanisms.

### F17. Drag/drop and reorder adapters — P2

Requirements:

- Flat move/copy by stable item key.
- SourceList index move and SourceCache order-key strategies.
- Hierarchical reparent/reorder with parent-key and cycle validation.
- Typed target validation and operation selection hooks.
- Command/interaction output for domain-owned mutation.
- Generated session status suitable for badges and diagnostics.
- Header-only, row, cell, or custom drag-handle policies.
- Selection-drag coordination.

The default generated adapter should request a move from a ViewModel service. It must not guess how domain collections should be mutated.

### F18. Pivot, outline, formula, and chart metadata — P3

These integrations should be capability-gated so projects that reference only the core grid do not see or compile analytics output.

Pivot requirements:

- Typed row/column/filter/value field selectors.
- Date/numeric grouping, sort, value filters, slicers, missing-item policy, and layout defaults.
- Typed calculated-measure dependencies and custom aggregate factories.
- Display modes including percent, running total, difference, parent percent, and index.

Outline requirements:

- Typed group/detail fields, subtotal fields, and expansion keys.
- Generated bindings for outline row projections.

Formula requirements:

- Stable formula names and column dependency metadata.
- Static formula syntax validation when the optional formula analyzer package is present.
- Generated typed value resolver/setter tables.
- A1/structured reference metadata and relative-reference support for fill.
- User code remains responsible for custom functions and dynamic formula text.

Chart requirements:

- Typed category/value/series selectors.
- Range-to-series projection for spreadsheet selection.
- Keyed incremental chart updates from the same source pipeline.
- Grid/chart selection and current-item synchronization.
- User-defined chart source/renderer implementations remain first-class.

### F19. Generated view recipes and event bridges — P1/P3

The current generated grid-only Avalonia and ReactiveUI views should expand through a small recipe set:

- `GridOnly`
- `SearchableGrid`
- `OperationsToolbar`
- `Explorer`
- `Spreadsheet`
- `Analytics`
- `MasterDetail`

**Proposed extension:**

```csharp
[GenerateDataGridView(
    typeof(Trade),
    ViewName = "TradeBlotterView",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.OperationsToolbar,
    ControllerName = "Trades",
    BaseType = typeof(WorkspaceViewBase<>),
    AutomationId = "trade-blotter")]
public sealed partial class TradeBlotterViewModel;
```

Requirements:

- Bind against the grouped controller property.
- Use Avalonia binding indexers/compiled paths only.
- Generate ReactiveUI activation and adapter lifetime wiring.
- Bind commands instead of emitting code-behind event handlers.
- Bridge routed grid events to generated ViewModel commands/interactions when requested.
- Support custom base types, themes, resource keys, toolbar slots, empty/loading/error states, diagnostics status, and row details.
- Emit reflection-free view registration.
- Allow multiple views per ViewModel.

Custom frameworks cannot be loaded as arbitrary compiler plugins from user code. A new framework strategy should be delivered as a compatible generator package/strategy. User customization within an existing strategy uses base types, runtime adapters, named partial hooks, and implementation types.

### F20. Localization, accessibility, diagnostics, and test metadata — P3

Requirements:

- Header/description resource keys and direct strongly typed resource access when configured.
- Culture-aware generated format defaults.
- Stable automation IDs for grid, columns, filter editors, search, and generated toolbar commands.
- Automation names/help text from explicit metadata or localized headers.
- A generated debug manifest showing accessor coverage, operation ownership, active fallbacks, schema version, and stream metrics.
- Optional generated test-data builder metadata, but not generated business test cases.
- Headless tests must be able to locate generated controls without visual-tree reflection.

### F21. Collection views, paging, currency, and auto-column replacement — P2

Requirements:

- Generate a typed `DataGridCollectionView` factory for local enumerable/observable sources.
- Apply generated grouping, paging, and currency/current-item defaults.
- Generate add/delete/new-row policies only when a user service owns creation and mutation.
- Preserve current item and selection by stable key when pages or sources change.
- Convert runtime auto-generation into a build-time schema when the item type is statically known.
- Generate the `AutoGeneratingColumn` customization as schema metadata rather than a view event handler.
- Support interfaces and explicit-interface properties when the target member can be resolved statically.
- Detect `DataTable`, `ICustomTypeDescriptor`, dictionaries, and runtime property bags as dynamic shapes and require an explicit runtime accessor provider.
- Generate range-aware adapters for add/remove/replace/move/reset so bulk updates are not expanded into avoidable per-item work.

The generator must not pretend that an unknown runtime schema is compile-time typed. Dynamic shapes use a clearly labeled runtime provider and are excluded from strict typed accessor guarantees.

### F22. Header menus, filter editors, and distinct-value providers — P2

Requirements:

- Generate per-field filter-editor metadata from the field type and explicit overrides.
- Text, numeric, date/time, boolean, enum, range, and distinct-value editor profiles.
- Generate local distinct-value enumeration using typed accessors.
- Support async/server distinct-value providers keyed by generated field ID and current query context.
- Cancellation, debounce, result limits, and stale-response suppression for remote values.
- Generate header-menu commands for sort, clear sort, filter, clear filter, visibility, pin/freeze, autosize, and reset layout.
- Integrate generated column chooser and band metadata.
- Allow a user-provided editor factory, flyout factory, or resource key per field.
- Keep all visual styling in Avalonia resources/templates.

Distinct-value generation must be bounded and should not scan an unbounded live source on the UI thread.

Implemented command API:

```csharp
using DataGridGeneratedOperationController<Trade> operations =
    TradeGridSchema.CreateController();
using DataGridGeneratedColumnLayoutController layout =
    TradeGridSchema.CreateColumnLayoutController();
using DataGridGeneratedHeaderCommandController<Trade> headers =
    TradeGridSchema.CreateHeaderCommandController(
        operations,
        layout,
        interaction: new TradeGridHeaderInteraction());

DataGridGeneratedHeaderCommandSet price = headers.ForField("price");
price.SortDescending.Execute(null);
price.ClearFilter.Execute(null);
price.HideColumn.Execute(null);
price.PinLeft.Execute(null);
price.FreezeThrough.Execute(null);
price.AutoSize.Execute(null);
price.ResetLayout.Execute(null);
```

`IDataGridGeneratedHeaderInteraction` is the UI boundary for pin, freeze, autosize,
and any grid-instance behavior. Applications may replace that small interface or the
complete `IDataGridGeneratedHeaderCommandHandler`. Sort, filter, visibility, and
layout-reset operations use generated field IDs and existing typed controllers. No
reflection, expression compilation, or DataGrid reference is introduced in the
ViewModel.

### F23. Virtualization, scrolling, input, and diagnostics profiles — P2

Source generation cannot optimize the DataGrid renderer by itself, but it can make performance-sensitive configuration explicit and consistent.

Requirements:

- Named profiles for uniform rows, estimated variable height, measured variable height, spreadsheet, tree, and high-frequency streaming.
- Generate row-height estimator, cache-key, template-reuse, logical-scrolling, frozen-column, and virtualization configuration where the runtime API supports it.
- Validate incompatible settings such as an unbounded details template in a high-frequency profile.
- Generate keyboard gesture maps and command bridges for common navigation/edit/search/fill operations.
- Generate XY-focus and current-cell bindings for code-only view recipes.
- Generate scroll/state interactions without exposing the DataGrid instance to the ViewModel.
- Expose row realization/recycling, update queue, search index, hierarchy flatten, and generated pipeline metrics through the diagnostics manifest.
- Allow user-defined row-height estimator, cache, input-map, and diagnostics sink implementation types.

Generated profiles are presets, not hidden heuristics. Explicit user properties always win, and the active settings appear in diagnostics.

## 8. End-to-end proposed usages

### 8.1 ReactiveUI + DynamicData `SourceCache`

```csharp
[GenerateDataGridColumns(
    ProviderName = "TradeGridSchema",
    Strict = true,
    Streaming = true)]
public sealed class Trade
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "ID",
        ColumnKey = "trade-id",
        IsReadOnly = true)]
    public int Id { get; init; }

    [DataGridColumn(DataGridColumnKind.Text,
        Header = "Symbol",
        ColumnKey = "trade-symbol")]
    public required string Symbol { get; init; }

    [DataGridColumn(DataGridColumnKind.Text,
        Header = "Desk",
        ColumnKey = "trade-desk")]
    public required string Desk { get; init; }

    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "Price",
        ColumnKey = "trade-price",
        FormatString = "N2")]
    [DataGridSummary(DataGridAggregateType.Average, Format = "N2")]
    public decimal Price { get; init; }
}

[GenerateDataGridController(
    typeof(Trade),
    "Trades",
    ProviderName = "TradeGridSchema",
    SourceMember = nameof(_source),
    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceCache,
    OperationExecution = DataGridOperationExecution.ExternalPipeline,
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Sorting |
               DataGridGeneratedFeatures.Filtering |
               DataGridGeneratedFeatures.Searching |
               DataGridGeneratedFeatures.Selection |
               DataGridGeneratedFeatures.Summaries |
               DataGridGeneratedFeatures.State,
    Streaming = true)]
[GenerateDataGridView(
    typeof(Trade),
    ViewName = "TradeBlotterView",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.OperationsToolbar,
    ControllerName = "Trades")]
public sealed partial class TradeBlotterViewModel : ReactiveObject, IDisposable
{
    private readonly SourceCache<Trade, int> _source =
        new(static trade => trade.Id);

    [Reactive]
    private string _query = string.Empty;

    [Reactive]
    private string _desk = string.Empty;

    public TradeBlotterViewModel()
    {
        InitializeTrades(TradeBlotterViewModelGenerated.CreateTrades(
            _source,
            RxSchedulers.MainThreadScheduler));

        this.WhenAnyValue(x => x.Query)
            .Throttle(TimeSpan.FromMilliseconds(150))
            .Subscribe(Trades.Search.SetText)
            .DisposeWith(Trades.Lifetime);

        this.WhenAnyValue(x => x.Desk)
            .Subscribe(value => Trades.Filter.Set(
                TradeGridSchema.Desk.Contains(value)))
            .DisposeWith(Trades.Lifetime);
    }

    public void Dispose()
    {
        Trades.Dispose();
        _source.Dispose();
    }
}
```

Generated output owns descriptor-to-predicate/comparer translation, DynamicData subjects, operation subscriptions, `SortAndBind`, read-only output collection, adapter factories, metrics, and error propagation. The user owns source mutation and ViewModel lifetime.

### 8.2 Custom implementation and configure hook

```csharp
[GenerateDataGridController(
    typeof(AuditEntry),
    "Audit",
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Searching |
               DataGridGeneratedFeatures.State,
    ImplementationType = typeof(AuditGridControllerFactory),
    ConfigureMethod = nameof(ConfigureAuditGrid))]
public sealed partial class AuditViewModel : ReactiveObject
{
    private static void ConfigureAuditGrid(
        ref DataGridGeneratedControllerOptions<AuditEntry> options)
    {
        options.Search.Debounce = TimeSpan.FromMilliseconds(75);
        options.State.Version = 3;
    }
}

public sealed class AuditGridControllerFactory :
    IDataGridGeneratedControllerFactory<AuditEntry>
{
    public IDataGridGeneratedController<AuditEntry> Create(
        in DataGridGeneratedControllerContext<AuditEntry> context)
    {
        return new AuditGridController(context);
    }
}
```

The generator validates the interface, constructor/factory shape, accessibility, generic item type, and nullability before emitting the call.

### 8.3 Hierarchical streaming explorer

```csharp
[GenerateDataGridColumns(ProviderName = "FileNodeSchema", Streaming = true)]
public sealed class FileNode
{
    [DataGridKey]
    public required string Path { get; init; }

    [DataGridChildren]
    public ObservableCollection<FileNode> Children { get; } = [];

    [DataGridExpanded]
    public bool IsExpanded { get; set; }

    [DataGridColumn(DataGridColumnKind.Hierarchical,
        Header = "Name",
        ColumnKey = "name")]
    public required string Name { get; init; }

    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "Size",
        ColumnKey = "size")]
    [DataGridSummary(DataGridAggregateType.Sum)]
    public long Size { get; init; }
}

[GenerateDataGridController(
    typeof(FileNode),
    "Files",
    SourceMember = nameof(Roots),
    SourceKind = DataGridGeneratedSourceKind.ObservableCollection,
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Hierarchy |
               DataGridGeneratedFeatures.Sorting |
               DataGridGeneratedFeatures.Filtering |
               DataGridGeneratedFeatures.Searching |
               DataGridGeneratedFeatures.Selection |
               DataGridGeneratedFeatures.State,
    Streaming = true)]
public sealed partial class FileExplorerViewModel : ReactiveObject
{
    public ObservableCollection<FileNode> Roots { get; } = [];
}
```

The generated Avalonia view binds to a typed generated node projection, so it does not require `x:CompileBindings="False"`.

### 8.4 Remote data

```csharp
[GenerateDataGridController(
    typeof(Customer),
    "Customers",
    SourceMember = nameof(_provider),
    SourceKind = DataGridGeneratedSourceKind.Remote,
    OperationExecution = DataGridOperationExecution.Remote,
    KeyMember = nameof(Customer.Id),
    Features = DataGridGeneratedFeatures.Columns |
               DataGridGeneratedFeatures.Sorting |
               DataGridGeneratedFeatures.Filtering |
               DataGridGeneratedFeatures.Searching |
               DataGridGeneratedFeatures.Selection |
               DataGridGeneratedFeatures.State)]
public sealed partial class CustomerSearchViewModel : ReactiveObject
{
    private readonly IDataGridQueryProvider<Customer, Guid> _provider;

    public CustomerSearchViewModel(
        IDataGridQueryProvider<Customer, Guid> provider)
    {
        _provider = provider;
        InitializeCustomers(
            CustomerSearchViewModelGenerated.CreateCustomers(provider));
    }
}
```

The controller owns debounce, cancellation, page state, stale-response suppression, and UI-neutral loading/error state. The provider owns query translation and transport.

### 8.5 Assembly and namespace policy

```csharp
[assembly: GenerateDataGridColumnsForNamespace(
    "Contoso.Trading.Models",
    IncludeChildNamespaces = true,
    Strict = true,
    Streaming = true)]

[assembly: GenerateDataGridControllersForNamespace(
    "Contoso.Trading.ViewModels",
    ItemNamespace = "Contoso.Trading.Models",
    Framework = DataGridControllerFramework.ReactiveUI,
    DefaultFeatures = DataGridGeneratedFeatures.Columns |
                      DataGridGeneratedFeatures.Sorting |
                      DataGridGeneratedFeatures.Filtering |
                      DataGridGeneratedFeatures.Searching |
                      DataGridGeneratedFeatures.State)]
```

Namespace policy supplies defaults only. An explicit type/controller attribute may opt out or override any setting. Ambiguous ViewModel-to-item matching is an error; the generator does not guess from similar names.

### 8.6 Runtime indexed spreadsheet columns

```csharp
[GenerateDataGridIndexedColumns(
    Name = "Cells",
    GetterMethod = nameof(GetCell),
    SetterMethod = nameof(SetCell),
    NotificationNameMethod = nameof(GetCellPropertyName))]
public sealed partial class SpreadsheetRow : ReactiveObject
{
    private readonly object?[] _cells;

    public object? GetCell(int index) => _cells[index];

    public void SetCell(int index, object? value)
    {
        if (Equals(_cells[index], value))
        {
            return;
        }

        _cells[index] = value;
        this.RaisePropertyChanged(GetCellPropertyName(index));
    }

    public static string GetCellPropertyName(int index) =>
        ExcelColumnName.FromIndex(index);
}

DataGridColumnDefinition price = SpreadsheetRowCells.CreateColumn<decimal>(
    index: 2,
    new DataGridIndexedColumnOptions<decimal>
    {
        Header = "C",
        ColumnKey = "C",
        Kind = DataGridColumnKind.Numeric,
        FormatString = "N2"
    });
```

The generated accessor and binding path are cached per slot and use direct method calls.

## 9. Diagnostics

Proposed diagnostic range for expansion work:

| ID | Default | Condition |
|---|---|---|
| `PDGSG100` | Error | Duplicate or empty stable field/column key. |
| `PDGSG101` | Error | Invalid/missing key member or incompatible composite-key method. |
| `PDGSG102` | Error | Generated controller/member name collision. |
| `PDGSG103` | Error | Source member type does not match configured source kind. |
| `PDGSG104` | Error | Conflicting operation owners or double-application configuration. |
| `PDGSG105` | Error | Custom hook/factory signature is invalid. |
| `PDGSG106` | Error | Required optional integration assembly is not referenced. |
| `PDGSG107` | Warning | Generated pipeline has no recognized disposal/activation owner. |
| `PDGSG108` | Warning | Streaming output has no explicit scheduler or UI boundary. |
| `PDGSG109` | Error | Invalid hierarchy children/expanded/parent-key configuration. |
| `PDGSG110` | Warning | Summary cannot update incrementally and will reset/recompute. |
| `PDGSG111` | Error | Persisted state requested without stable item and column keys. |
| `PDGSG112` | Warning | Known template/theme/resource key is missing from an optional resource manifest. |
| `PDGSG113` | Error | Strict generated path would require reflection or dynamic code. |
| `PDGSG114` | Error | Remote query provider has an incompatible item/key type. |
| `PDGSG115` | Error | Generated view binding target is missing or has an incompatible type. |
| `PDGSG116` | Error | Custom implementation is inaccessible, abstract, open generic, or incompatible. |
| `PDGSG117` | Error | Duplicate controller feature declaration for the same name. |
| `PDGSG118` | Warning | Async stream uses an unbounded buffer without explicit opt-in. |
| `PDGSG119` | Error | Namespace convention produces an ambiguous ViewModel/item/view match. |
| `PDGSG120` | Warning | Hierarchical compiled-binding projection is unavailable and runtime binding would be required. |

Strict mode promotes applicable fallback warnings to errors. Diagnostics should point to the smallest relevant attribute argument or member declaration and include the expected signature/type.

## 10. Performance specification

### 10.1 Generator performance

Required properties:

- No full source-text scans outside attribute candidates.
- No semantic model requests for files without relevant candidates.
- No global compilation dependency for type-scoped generation.
- No nondeterministic ordering.
- No generated timestamp or machine-specific path.
- Cached immutable semantic models with structural equality.
- Separate output nodes so view edits do not invalidate unrelated schemas.

Benchmark scenarios:

1. 1,000 annotated row types, cold generation.
2. One property edit in one row type.
3. One generated-view recipe edit.
4. One assembly/namespace policy edit.
5. 100 controllers using the same row schema.
6. Design-time compilation with incomplete code.

Performance gates should be based on tracked repository baselines and allocations, not fragile universal millisecond targets.

### 10.2 Runtime hot paths

Required properties:

- Zero reflection and zero runtime expression compilation in strict generated paths.
- No per-row allocation during sort comparisons, filter evaluation, search evaluation, key lookup, or simple summary updates after warm-up.
- No primitive boxing when a typed operator/accessor path is available.
- Descriptor compilation once per descriptor revision, not per row.
- Incremental collection changes remain incremental through the final bound collection.
- One UI scheduler hop per emitted batch.
- Bounded stream buffers and bounded key/index caches.
- Deterministic disposal releases sources, subjects, timers, adapters, and event handlers.

Benchmark suites:

- Generated accessor get/set versus current compiled binding and reflection fallback.
- Sort comparer construction and 1M comparisons.
- Filter/search predicate construction and 1M evaluations.
- SourceList and SourceCache update throughput at 10k/100k rows.
- Key/index selection resolution after add/remove/move/replace/reset.
- Hierarchical flattening and expand/collapse under range changes.
- Incremental summaries and grouping.
- Snapshot reconciliation for the market-dashboard shape.
- Indexed spreadsheet cell access/fill/paste.
- Generated view creation/activation/deactivation.

Any performance claim in documentation must include the benchmark project, runtime, configuration, data shape, median, distribution, and allocation result.

## 11. Testing specification

### 11.1 Generator unit tests

Use xUnit and Roslyn generator-driver tests for:

- Every trigger scope: property, type, ViewModel, view, namespace, and assembly.
- Every feature attribute and precedence rule.
- Exact generated source or focused structural assertions.
- Every diagnostic and recovery path.
- Deterministic hint names/output.
- Incremental step caching after unrelated and related edits.
- Incomplete syntax and design-time errors.
- Custom implementation/hook signature validation.
- Multiple controllers and views per ViewModel.
- Cross-assembly public manifest consumption.

### 11.2 Runtime unit tests

- Typed operator parity with existing sorting/filtering/search semantics.
- Null, nullable, enum, numeric, date/time, culture, and custom comparer cases.
- Operation ownership and no double application.
- Key/index cache correctness under every collection-change action.
- Stream batching, overflow, cancellation, error, and disposal.
- Remote query revision and stale-response behavior.
- Hierarchical key, path, expansion, selection, and cycle behavior.
- Incremental summary parity with full recomputation.
- State versioning and migrations.
- Clipboard/fill/edit conversion and validation.

### 11.3 Avalonia Headless tests

- Generated views for plain Avalonia and ReactiveUI.
- Compiled bindings enabled, including hierarchical projections.
- Keyboard/pointer selection, editing, clipboard, fill, drag/drop, and routed event bridges.
- Activation/deactivation and no duplicate subscriptions.
- State capture/restore through the generated view adapter.
- Automation IDs and accessible names.
- Screenshot coverage only where visual composition is part of the contract.

### 11.4 Integration and deployment tests

- DynamicData SourceList and SourceCache end-to-end tests.
- Optional integration absent/present reference tests.
- `dotnet publish -p:PublishAot=true` smoke applications.
- Trimming warnings treated as errors for generated sample projects.
- No runtime binding warnings in strict generated samples.
- Memory-leak tests for generated views/controllers after detach/dispose.

## 12. Implementation plan

### Phase 0 — generator foundation

1. Split discovery into attribute-driven incremental pipelines.
2. Introduce equatable schema/field/controller/view semantic models.
3. Emit versioned schema manifests and preserve current outputs.
4. Add generator performance tests and incremental-caching tests.
5. Add the abstractions/runtime boundary needed for cross-assembly manifests.

Exit criteria: existing generator tests pass unchanged; one-type edits do not regenerate unrelated type outputs; deterministic-output and performance baselines exist.

### Phase 1 — identity and operation core

1. Add `[DataGridKey]` and generated key/index services.
2. Add typed operation descriptors/builders and canonical field IDs.
3. Add typed local collection-view construction and range-aware adapters.
4. Add the UI-neutral generated operation controller.
5. Add Avalonia adapters to existing sorting/filtering/search models.
6. Add generated presets, ownership diagnostics, and strict no-reflection enforcement.

Exit criteria: the sorting/filtering/searching model samples can be rewritten without manual property-path switches or model-event subscriptions.

### Phase 2 — reactive and live data

1. Add DynamicData SourceList support.
2. Add SourceCache support using generated keys.
3. Add hierarchical DynamicData support.
4. Add async-enumerable/channel ingestion and keyed snapshot reconciliation.
5. Add scheduler, backpressure, metrics, errors, cancellation, and disposal.
6. Add ReactiveUI activation and command strategy.

Exit criteria: all eight DynamicData sample ViewModels use generated pipelines; the generated trade sample no longer owns manual subjects/event handlers; streaming and disposal tests pass.

### Phase 3 — hierarchy, selection, and state

1. Generate hierarchy options and typed node projections.
2. Convert hierarchical sample views back to compiled bindings.
3. Generate selection and fast index resolution.
4. Generate state key maps, interactions/adapters, versions, and migrations.
5. Add shared grid/chart/control selection bridges.

Exit criteria: representative hierarchy, grouped selection, paging selection, selection fast-index, and full-state pages run without reflection binding or view-owned state logic.

### Phase 4 — editing workflows

1. Generate edit/conversion/validation policies.
2. Generate clipboard import/export adapters.
3. Generate fill models and formula-relative fill hooks.
4. Generate optional undo transaction output.
5. Add indexed/method-backed column families.

Exit criteria: the Excel sample removes its generic binding helper and most selection/clipboard/fill bridge boilerplate without losing spreadsheet-specific customization.

### Phase 5 — grouping, summaries, formatting, and layout

1. Add typed group descriptions.
2. Add generated and incremental summaries.
3. Add conditional formatting predicates.
4. Add bands, chooser, frozen placement, width groups, and layout profiles.
5. Add header menus, filter-editor metadata, and bounded distinct-value providers.
6. Add template/custom-drawing factory metadata.
7. Add explicit virtualization, row-height, input, and diagnostics profiles.

Exit criteria: grouping, summary, conditional-formatting, banding, chooser, and custom-drawing samples each have generated equivalents and parity tests.

### Phase 6 — views and application integration

1. Expand generated view recipes and event-to-command bridges.
2. Add reflection-free view/schema registries and DI registration.
3. Add localization, automation metadata, loading/error/empty states, and diagnostics panel bindings.
4. Migrate representative generated samples to current ReactiveUI source generators.

Exit criteria: generated views require no handwritten event handlers; ReactiveUI views activate/dispose correctly; another framework can be added without modifying schema discovery.

### Phase 7 — analytics integrations

1. Add pivot and outline metadata generation.
2. Add optional formula analyzer/value resolver generation.
3. Add chart series/range/selection projections.
4. Add drag/drop generated adapters.
5. Add capability-gated integration tests and benchmarks.

Exit criteria: pivot, outline, chart, formula, and drag/drop sample families each demonstrate typed generated metadata with custom implementation escape hatches.

### Continuous validation lane — ProDiagnostics

1. Keep every ProDiagnostics DataGrid on generated column definitions and fast-path options.
2. Exercise flat, template, hierarchical, shared-schema, multi-schema, streaming, and layout-controller paths.
3. Keep existing XAML view activation on generated registrations rather than naming reflection.
4. Run ProDiagnostics unit and Avalonia Headless tests for every generator change.
5. Record intentional inspector reflection separately from generated application wiring and never use it as a silent grid fallback.

Exit criteria: both ProDiagnostics assemblies build for every target framework, all ProDiagnostics tests pass, registry manifests match the expected schema set, registered views instantiate without reflection, and source audits contain no inline DataGrid columns or disabled compiled-binding scopes.

## 13. New sample plan

Add focused pages rather than one overloaded showcase:

1. `GeneratedOperationsControllerPage` — typed local sort/filter/search and presets.
2. `GeneratedDynamicDataSourceListPage` — live list, batching, sorting, filtering, and search.
3. `GeneratedDynamicDataSourceCachePage` — keyed updates and selection preservation.
4. `GeneratedHierarchicalDynamicDataPage` — typed compiled node bindings.
5. `GeneratedRemoteQueryPage` — cancellation, paging, stale responses, loading, and error state.
6. `GeneratedSelectionStatePage` — key/index cache and full state round-trip.
7. `GeneratedGroupingSummariesPage` — incremental grouped aggregates.
8. `GeneratedEditingClipboardFillPage` — typed validation, paste, and fill.
9. `GeneratedIndexedSpreadsheetPage` — runtime slot columns and formulas.
10. `GeneratedConditionalFormattingPage` — typed predicates and theme keys.
11. `GeneratedPivotChartPage` — typed pivot and chart projection.
12. `GeneratedReactiveViewRecipesPage` — grid-only, explorer, spreadsheet, and analytics recipes.
13. `GeneratedCustomImplementationsPage` — custom factory, base view, hook, comparer, validator, and summary calculator.
14. `GeneratedAssemblyNamespacePolicyPage` — assembly/namespace discovery and explicit overrides.
15. `GeneratedHeaderFiltersPage` — typed editors, local/remote distinct values, and header commands.
16. `GeneratedVirtualizationProfilePage` — variable-height estimates, recycling metrics, keyboard maps, and state-safe scrolling.

Each sample needs a ViewModel unit test. Interaction samples also need Avalonia Headless tests. Streaming samples need deterministic virtual-time tests and exposed metrics.

## 14. Acceptance criteria

The expansion is complete when:

- Existing generator APIs and generated source remain source-compatible.
- All new generation is incremental, deterministic, and cancellation-aware.
- Strict generated paths use no reflection or runtime expression compilation.
- DynamicData and async pipelines have explicit ownership, scheduler, backpressure, errors, and disposal.
- Stable keys are shared by selection, state, hierarchy, drag/drop, and chart coordination.
- Hierarchical generated samples use compiled bindings.
- ProDiagnostics remains a green production validation lane with generated schemas for every grid and generated view registration.
- Generated views are passive, command/interaction driven, and support custom base classes.
- ReactiveUI is the first full strategy; core schema/controller output remains framework neutral.
- Custom user implementations are compile-time validated and called directly.
- Every production path has xUnit coverage; UI interactions use Avalonia Headless.
- NativeAOT sample publication succeeds without generated-code trimming warnings.
- Benchmarks demonstrate that generated paths do not regress the existing fast path and materially reduce integration allocations/boilerplate in the audited scenarios.

## 15. Decisions to make before Phase 1

1. Whether public cross-assembly attributes ship in a new abstractions package or remain injected for same-compilation use with a separate manifest contract.
2. The exact UI-neutral descriptor/controller runtime shape and whether it uses System.Reactive abstractions.
3. Whether generated controllers are constructed explicitly, lazily, or through generated DI factories. Explicit construction is the safest initial lifetime model.
4. Whether typed group/summary accessors require additions to core DataGrid public APIs or can be adapted entirely through existing column value accessors.
5. The serialization strategy used for generated state metadata and migrations.
6. Which generated view recipes are stable enough for the first release; `GridOnly`, `SearchableGrid`, and `OperationsToolbar` should come first.
