# Samples and production validation

The repository validates generated APIs in focused sample pages, ProDiagnostics production applications, generator/runtime/headless tests, NativeAOT, and benchmarks. Use these scenarios as starting points for application designs.

## Core schema and column scenarios

| Scenario | Sample | Demonstrates |
| --- | --- | --- |
| Basic generated schema | `GeneratedColumnsDynamicDataPage` | Attributed columns, models, DynamicData, generated fast paths. |
| Assembly/namespace policy | `GeneratedAssemblyNamespacePolicyPage` | Namespace defaults, explicit overrides, registry, existing XAML view registration. |
| Interface contracts | Generator tests | Inherited interfaces, explicit implementations, ambiguity diagnostics. |
| Runtime-defined rows | Generator/runtime tests | `DataGridRuntimeSchemaAdapter<TItem>` and explicit providers. |
| Custom implementations | `GeneratedCustomImplementationsPage` | Custom column factory, schema hook, comparer, validation, summary, base/derived generated views. |

## Operations and data sources

| Scenario | Sample | Demonstrates |
| --- | --- | --- |
| Named operations | `GeneratedOperationsControllerPage` | Generated controller, presets, descriptor projections, commands, compiled view. |
| DynamicData list | `GeneratedDynamicDataSourceListPage` | Batch-aware `SourceList`, upstream sort/filter/search, errors and lifetime. |
| DynamicData cache | `GeneratedDynamicDataSourceCachePage` | Keyed replacement, replace-aware sorting, preserved selection. |
| Remote queries | `GeneratedRemoteQueryPage` | Offset paging, bounded cache, translation, cancellation, stale suppression, retry/view state. |
| Paging and currency | `PagingSelectionPage` | Generated collection-view defaults, identity/currency across pages and replacements. |
| Grouping/summaries | `GeneratedGroupingSummariesPage` | Typed groups, rendered totals/group summaries, incremental aggregate changes. |
| Header filtering | `GeneratedHeaderFiltersPage` | Editor profiles, local/remote distinct values, cached commands, grid-scoped interactions. |

## Hierarchy and selection/state

| Scenario | Sample | Demonstrates |
| --- | --- | --- |
| Hierarchical DynamicData | `GeneratedHierarchicalDynamicDataPage` | Generated hierarchy model/adapter, async expansion, filtering policy, compiled wrapper bindings. |
| Selection/state | `GeneratedSelectionStatePage` | Extended selection, paging/replacement, all state sections, aliases and migration. |
| Shared grouped selection | `GeneratedGroupedSharedSelectionPage` | One generated identity selection model shared by DataGrid and ListBox. |

## Editing, transfer, layout, and rendering

| Scenario | Sample | Demonstrates |
| --- | --- | --- |
| Editing/clipboard/fill | `GeneratedEditingClipboardFillPage` | Typed edit fields, sync/async validation, projection, paste, series, formula fill, undo/export. |
| Conditional formatting | `GeneratedConditionalFormattingPage` | Typed cell/row rules, priorities, custom predicates, runtime rule toggling. |
| Indexed spreadsheet | `GeneratedIndexedSpreadsheetPage` | Runtime indexed family, typed slot notifications, formula slots, generated spreadsheet view. |
| Custom drawing editing | `CustomDrawingEditingPage` | Drawn/direct columns, generated custom-drawing accessor/cache options. |
| Row details | Generated row-details tests/pages | Resource, implementation, factory, and typed nested-grid recipes. |

## Analytics and application integration

| Scenario | Sample | Demonstrates |
| --- | --- | --- |
| Pivot/chart | `GeneratedPivotChartPage` | Ordered fields, calculated measure, pivot result chart, range chart, long-form series, selection sync. |
| Outline/drag-drop | `GeneratedOutlineDragDropPage` | Outline hooks, custom aggregate, domain-owned Move/Copy and refresh. |
| Reactive view recipes | `GeneratedReactiveViewRecipesPage` | GridOnly, Explorer, Spreadsheet, Analytics over one ViewModel/schema. |
| Virtualization/input/metrics | `GeneratedVirtualizationInputMetricsPage` | Performance profiles, input map, navigation, scroll state, renderer metrics. |
| Reactive view states/events | Generated remote/event samples | Loading/error/empty states, selection/cell event bridges, interactions. |

## ProDiagnostics as a production validation lane

ProDiagnostics is intentionally not a synthetic demo. It validates generated schemas and reflection-free view mapping in a multi-grid diagnostics application with arbitrary-object inspection at its domain boundary.

| Surface | Generated contract | Purpose |
| --- | --- | --- |
| Viewer metrics | Streaming keyed template schema, fast path, layout controller | High-frequency rows, numeric formatting, trend cells, chooser. |
| Viewer activities | Second named ViewModel projection | Multiple schemas on one ViewModel. |
| Assets | Attributed-only schema | Read-only sortable grid. |
| Control properties | Shared editable template schema/layout | Recycling edit cells and runtime column profiles. |
| Resource details | Second projection of shared schema | Per-view layout over one item schema. |
| Resource picker | Text/template schema | External collection-view operations. |
| Resources | Flat and hierarchical schemas on one screen | Multi-schema ViewModel and hierarchy. |
| Visual/logical tree | Hierarchical-row schema | `HierarchicalNode.Item` compiled bindings. |

ProDiagnostics uses a generated registry and explicit `[DataGridViewRegistration]` mappings. Its owned grids use generated definitions and compiled binding scopes. Reflection remains only where the diagnostics domain intentionally inspects unknown third-party runtime objects; it is not used for DataGrid binding or view location.

## NativeAOT smoke application

`tests/ProDataGrid.SourceGeneration.AotSmoke` is a generated-only self-contained executable. It covers:

- strict attributed schema and typed operations;
- generated Avalonia and ReactiveUI views;
- custom generated-view base;
- accessor-only fast paths;
- reflection-free registry;
- native executable startup and generated API execution.

CI rejects trimming/AOT warnings originating in emitted generator code and runs the native binary after publishing.

## Benchmark suite

`tests/ProDataGrid.SourceGeneration.Benchmarks` provides:

- semantic equivalence guards;
- generated vs equivalent handwritten compiled column creation;
- typed accessor, sorting, filtering, and searching benchmarks;
- cold/no-op/one-schema-edit generator benchmarks;
- one-schema and multi-schema workloads.

Run correctness validation before timing:

```bash
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --validate
```

## Test project map

| Project | Coverage |
| --- | --- |
| `src/ProDataGrid.SourceGenerators.UnitTests` | Discovery, model normalization, source output, diagnostics, compilation, incremental scenarios. |
| `src/Avalonia.Controls.DataGrid.UnitTests` | Generated runtime contracts, operations, identity/state, streams, remote queries, hierarchy, editing, transfer, analytics, metrics. |
| `src/DataGridSample.UnitTests` | Generated sample ViewModels and Avalonia Headless pages. |
| `src/ProDiagnostics.UnitTests` | Production schema registry, generated view creation, application behavior. |
| `tests/ProDataGrid.SourceGeneration.AotSmoke` | Trimming/NativeAOT publish and executable smoke. |
| `tests/ProDataGrid.SourceGeneration.Benchmarks` | Correctness and performance baselines. |

## Repository validation commands

```bash
dotnet build -c Release --no-restore -p:VersionSuffix=-build.local

dotnet test src/ProDataGrid.SourceGenerators.UnitTests/ProDataGrid.SourceGenerators.UnitTests.csproj -c Release --no-build

dotnet test src/Avalonia.Controls.DataGrid.UnitTests/Avalonia.Controls.DataGrid.UnitTests.csproj -c Release --no-build

dotnet test src/DataGridSample.UnitTests/DataGridSample.UnitTests.csproj -c Release --no-build

dotnet test src/ProDiagnostics.UnitTests/ProDiagnostics.UnitTests.csproj -c Release --no-build

dotnet run -c Release --no-build --no-restore --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --validate
```

Use the dedicated AOT commands from [Accessibility, diagnostics, performance, and validation](diagnostics-performance-testing.md#nativeaot-validation) for the current runtime identifier.

## Selecting a sample

- Start with `GeneratedOperationsControllerPage` for an ordinary reactive grid.
- Use `GeneratedDynamicDataSourceCachePage` for keyed live data.
- Use `GeneratedRemoteQueryPage` for server-owned operations.
- Use `GeneratedHierarchicalDynamicDataPage` for trees.
- Use `GeneratedEditingClipboardFillPage` for spreadsheet-style editing.
- Use `GeneratedCustomImplementationsPage` when generated defaults need application ownership.
- Use the ProDiagnostics migration as the reference for multi-grid production adoption.
