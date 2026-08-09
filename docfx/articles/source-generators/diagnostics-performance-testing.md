# Accessibility, diagnostics, performance, and validation

Generated metadata makes reflection-free behavior inspectable and testable. This article covers localization/accessibility, performance profiles, renderer metrics, generator diagnostics, incremental behavior, NativeAOT, benchmarks, and recommended tests.

## Localization providers

Use resource keys when Avalonia resources own localization, or direct validated methods for strongly typed localization:

```csharp
[DataGridColumn(
    Header = "Amount",
    Description = "Order amount",
    HeaderProviderMethod = nameof(GetAmountHeader),
    DescriptionProviderMethod = nameof(GetAmountDescription),
    HeaderResourceKey = "OrderAmountHeader",
    DescriptionResourceKey = "OrderAmountDescription")]
public decimal Amount { get; set; }

public static string GetAmountHeader(IFormatProvider provider) =>
    TradingResources.Amount;

public static string GetAmountDescription() =>
    TradingResources.AmountDescription;
```

Providers are called directly with `CurrentUICulture` where an `IFormatProvider` parameter is declared. `ResolveHeader` and `ResolveDescription` expose the same generated behavior to other UI surfaces.

## Accessibility metadata

Column metadata supports stable `AutomationId`, `AutomationName`, and `AutomationHelpText`. Generated views derive deterministic IDs for the view, grid, title, search, state surfaces, recipe slots, retry command, and built-in nested row details.

The title is exposed as a level-one automation heading. The DataGrid receives an accessible name/help text. Generated template roots should use the provided stable metadata rather than visual-tree position.

Headless tests can locate generated surfaces by automation ID without reflection or fragile tree indexes.

## Diagnostics manifest

Every generated schema exposes an immutable `DataGridGeneratedDiagnosticsManifest` containing:

- schema ID and hash;
- item type;
- strict/streaming configuration;
- selected performance profile;
- stable-key availability;
- field keys, value types, write/search/filter/analytics coverage;
- explicitly active compatibility fallbacks;
- generated/runtime metric names.

Use `HasFallbacks` as a production assertion for strict schemas:

```csharp
DataGridGeneratedDiagnosticsManifest diagnostics =
    TradeSchema.Diagnostics;

Debug.Assert(diagnostics.Strict);
Debug.Assert(!diagnostics.HasFallbacks);
```

## Performance profiles

`DataGridGeneratedPerformanceProfile` selects explicit runtime settings:

| Profile | Intended workload |
| --- | --- |
| `Balanced` | General-purpose logical scrolling with variable-height estimation. |
| `UniformRows` | Fixed 28-pixel rows. |
| `VariableHeightEstimated` | Highly variable rows with advanced estimation. |
| `VariableHeightMeasured` | Variable rows with measured-height caching. |
| `Spreadsheet` | Dense fixed 24-pixel rows and spreadsheet input map. |
| `Tree` | Hierarchical flattening and variable-height estimation. |
| `HighFrequencyStreaming` | Fixed 26-pixel rows and reduced search-change tracking. |

Apply on a schema or generated view:

```csharp
[GenerateDataGridColumns(
    PerformanceProfile =
        DataGridGeneratedPerformanceProfile.HighFrequencyStreaming,
    Streaming = true)]
public sealed class Quote { }

[GenerateDataGridView(
    typeof(Quote),
    PerformanceProfile =
        DataGridGeneratedPerformanceProfile.HighFrequencyStreaming)]
public sealed partial class QuotesViewModel { }
```

The generated view applies the profile before `ConfigureGeneratedDataGrid`, so an explicit application override wins. Override `CreateGeneratedPerformanceOptions` for a custom estimator or scrolling policy.

`HighFrequencyStreaming` with row details always visible reports `PDGSG128` because realizing details for every row contradicts the bounded profile.

## Input maps

`InputMapType` must implement `IDataGridGeneratedInputMap`. The built-in map always exposes platform-command+F for search; `Spreadsheet` additionally maps fill-down, fill-right, undo, and redo.

`DataGridGeneratedInputEvent<TItem>` forwards action, key/modifiers, typed current item, row/display-column indexes, and handled feedback to the ViewModel command.

## Renderer metric sinks

```csharp
[GenerateDataGridView(
    typeof(Trade),
    Framework = DataGridViewFramework.ReactiveUI,
    DiagnosticsSinkType = typeof(TradeMetricsSink))]
public sealed partial class TradesViewModel : ReactiveObject { }

public sealed class TradeMetricsSink : IDataGridGeneratedMetricsSink
{
    public void Record(
        in DataGridGeneratedMetricMeasurement measurement,
        ReadOnlySpan<KeyValuePair<string, object>> tags)
    {
        // Copy only data that must outlive this callback.
    }

    public void Dispose() { }
}
```

`DataGridGeneratedMetricsBridge` observes only `ProDataGrid.Diagnostic.Meter`. It forwards counter, up/down-counter, and histogram samples with schema ID and active profile. Metric tags remain a `ReadOnlySpan`; the bridge does not allocate a tag collection.

ReactiveUI views scope the subscription to activation. Plain Avalonia views scope it to visual-tree attachment. The subscription owns and disposes the sink.

Enable built-in instruments before DataGrid initialization:

```csharp
AppContext.SetSwitch("ProDataGrid.Diagnostics.IsEnabled", true);
```

The meter is process-wide. Generated schema/profile context identifies the subscribing view configuration, not necessarily the individual DataGrid that produced a built-in measurement.

## Compile-time diagnostics

| Code | Meaning |
| --- | --- |
| `PDGSG001` | Unsupported target type. |
| `PDGSG002` | No eligible columns. |
| `PDGSG003` | Unsupported attributed property shape. |
| `PDGSG004` | Invalid configuration/factory method. |
| `PDGSG005` | Required target/containing type is not partial. |
| `PDGSG006` | Requested generated member already exists. |
| `PDGSG007` | Custom schema implementation does not satisfy its contract. |
| `PDGSG008` | Namespace request matched no eligible types. |
| `PDGSG009` | Required, conflicting, or kind-incompatible column configuration. |
| `PDGSG010` | Requested item property is inaccessible. |
| `PDGSG011` | Namespace ViewModel policy cannot infer one item type. |
| `PDGSG012` | Generated-view binding member is missing. |
| `PDGSG013` | Generated-view custom base is invalid. |
| `PDGSG014` | Requested view framework is not referenced. |
| `PDGSG100` | Empty or duplicate stable column key. |
| `PDGSG101` | Invalid/ambiguous/conflicting key configuration. |
| `PDGSG103` | Named-controller source member is missing/incompatible. |
| `PDGSG104` | Source kind and operation owner conflict. |
| `PDGSG109` | Invalid/ambiguous hierarchy metadata. |
| `PDGSG117` | Named-controller collision. |
| `PDGSG118` | Invalid persisted schema/state metadata. |
| `PDGSG121` | Invalid formula name or stable-key dependency. |
| `PDGSG122` | Invalid/conflicting custom-drawing factory. |
| `PDGSG123` | Invalid/conflicting row-details/nested-grid source. |
| `PDGSG124` | Invalid/conflicting row command or content member. |
| `PDGSG125` | Invalid generated view-state projection. |
| `PDGSG126` | Invalid generated event flags or command member. |
| `PDGSG127` | Invalid ReactiveUI interaction/navigation declaration. |
| `PDGSG128` | Invalid performance/input/metrics configuration or incompatible high-frequency setting. |
| `PDGSG129` | Missing/incompatible clipboard or fill model. |
| `PDGSG130` | Missing/incompatible formula model. |
| `PDGSG131` | Missing/incompatible conditional-formatting model. |
| `PDGSG132` | Ambiguous same-name inherited interface property. |
| `PDGSG133` | Ambiguous same-name explicit-interface implementations. |
| `PDGSG134` | Runtime-defined shape lacks an explicit adapter. |
| `PDGSG135` | Invalid configured collection mutation handler. |
| `PDGSG136` | Invalid configured new-row factory. |
| `PDGSG137` | Invalid configured formula-fill translator. |
| `PDGSG138` | Invalid statically declared formula syntax. |
| `PDGSG139` | Invalid generated-view theme/class/status metadata. |
| `PDGSG140` | Invalid paging or initial-currency defaults. |

Diagnostics are attached to the configuration attribute/property where possible. Treat warnings/errors as contract feedback; do not suppress them to preserve a reflection-free claim.

## Incremental behavior

The generator uses isolated attributed pipelines for direct requests. Equatable normalized models and stable hint names keep output deterministic. Expensive schema discovery/emission is cached per schema.

Assembly/namespace policies and registries activate the compilation-wide coordination lane. An empty-policy gate prevents global source-type enumeration for ordinary direct-only consumers.

When measuring incremental behavior, distinguish:

- cold generator execution;
- unchanged compilation reuse;
- one-schema semantic edit;
- assembly/namespace policy edits that intentionally affect many owners.

## Generator unit tests

Use `GeneratorDriver` tests to cover:

- emitted source and compilation;
- every attribute family and generated member collision;
- diagnostics and exact source locations;
- class/struct/interface and explicit-interface schemas;
- customization method/type validation;
- assembly/namespace precedence and registry output;
- generated Avalonia and ReactiveUI view source;
- deterministic output and incremental changes.

Repository suite:

```bash
dotnet test src/ProDataGrid.SourceGenerators.UnitTests/ProDataGrid.SourceGenerators.UnitTests.csproj -c Release
```

## Runtime and headless tests

Runtime tests should execute generated factories/controllers—not only inspect strings. Cover operation equivalence, keyed selection/state, mutations, streaming bounds, stale remote responses, hierarchy transactions, editing/transfer, layouts, analytics, and event feedback.

Avalonia Headless tests should instantiate generated views, set typed DataContexts, activate ReactiveUI views, and verify:

- compiled bindings and model ownership;
- automation IDs and recipe slots;
- view-state and event/interaction lifetimes;
- hierarchy/model binding order;
- direct/drawn/template/detail configuration;
- deterministic screenshots for representative views.

## NativeAOT validation

`tests/ProDataGrid.SourceGeneration.AotSmoke` publishes and executes a generated-only NativeAOT app covering schema, controller, Avalonia view, ReactiveUI view, custom base, fast paths, and registry.

It enables trimming/AOT analysis and promotes generated-code `IL2026`, `IL2070`, `IL2075`, and `IL3050` warnings to failures.

Example for macOS Arm64:

```bash
dotnet restore tests/ProDataGrid.SourceGeneration.AotSmoke/ProDataGrid.SourceGeneration.AotSmoke.csproj -r osx-arm64
dotnet build tests/ProDataGrid.SourceGeneration.AotSmoke/ProDataGrid.SourceGeneration.AotSmoke.csproj -c Release -r osx-arm64 --no-restore
dotnet publish tests/ProDataGrid.SourceGeneration.AotSmoke/ProDataGrid.SourceGeneration.AotSmoke.csproj -c Release -r osx-arm64 --self-contained true --no-restore -p:WarningsAsErrors=
./tests/ProDataGrid.SourceGeneration.AotSmoke/bin/Release/net10.0/osx-arm64/publish/ProDataGrid.SourceGeneration.AotSmoke
```

Use the appropriate runtime identifier on other platforms.

## Benchmarks

`tests/ProDataGrid.SourceGeneration.Benchmarks` measures generated columns/accessors/operations against an equivalent handwritten compiled provider and measures cold/no-op/one-schema generator executions.

```bash
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --validate
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --anyCategories Runtime
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --anyCategories Generator
```

CI runs correctness guards, not timing thresholds. For meaningful comparisons, record commit/dirty state, SDK/runtime, OS/architecture, hardware/power state, command, BenchmarkDotNet version, distributions, and allocation columns. Retain `BenchmarkDotNet.Artifacts`.

## Production audit checks

For generated-only projects, add repository checks that prevent regression to:

- inline DataGrid columns where a generated schema owns them;
- disabled compiled-binding scopes;
- `Type.GetProperty`, expression compilation, `DynamicInvoke`, runtime XAML loading, or reflection-based view location in generated integration code;
- unbounded channels/caches;
- event handlers in passive views.

ProDiagnostics is the production validation lane for these rules; see [samples and production validation](samples-and-production-validation.md).
