# Registries and customization

The generator supports granular factories and hooks, complete implementation replacement, assembly/namespace defaults, reflection-free schema/view registries, optional Microsoft DI registration, and custom generated-view bases.

## Customization levels

Choose the smallest boundary that owns the required policy:

| Requirement | Boundary |
| --- | --- |
| Adjust one generated column | `DataGridColumn.ConfigureMethod` |
| Replace one column definition | `DataGridColumn.FactoryMethod` |
| Adjust the completed definition list | `GenerateDataGridColumns.ConfigureMethod` |
| Replace schema implementation | `GenerateDataGridColumns.ImplementationType` |
| Configure a named controller | `GenerateDataGridController.ConfigureMethod` |
| Replace controller construction | `GenerateDataGridController.ImplementationType` |
| Replace a generated view section | Protected virtual view hook |
| Share view infrastructure | `GenerateDataGridView.BaseType` |
| Resolve DI-backed feature services | Override protected generated factory |

Invalid hooks fail at compile time; the generator does not activate a method/type by name at runtime.

## Per-column configure method

```csharp
[DataGridColumn(
    DataGridColumnKind.Text,
    ConfigureMethod = nameof(ConfigureSymbol))]
public string Symbol { get; set; } = string.Empty;

public static void ConfigureSymbol(DataGridTextColumnDefinition column)
{
    column.Watermark = "Ticker";
}
```

The parameter type must match the concrete generated definition kind.

## Column factory

`FactoryMethod` replaces construction of one definition. If it returns a bound definition, it also owns that definition's initial binding. Use `DataGridColumnDefinitionBuilder.For<TItem>()`, `ClrPropertyInfo`, and typed delegates for a custom compiled-bound definition.

After factory construction, the generator applies canonical key/accessor metadata and declared options, then calls the per-column and schema-list configure hooks.

## Completed-list configuration

```csharp
[GenerateDataGridColumns(
    ProviderName = "TradeSchema",
    ConfigureMethod = nameof(ConfigureColumns))]
public sealed class Trade
{
    private static void ConfigureColumns(
        DataGridColumnDefinitionList columns)
    {
        for (int index = 0; index < columns.Count; index++)
        {
            columns[index].CanUserResize = true;
        }
    }
}
```

Use this for cross-column policy. Prefer declarative property metadata for stable schema semantics because it participates directly in diagnostics and hashing.

## Full schema implementation

```csharp
[GenerateDataGridColumns(
    ProviderName = "TradeSchema",
    ImplementationType = typeof(CustomTradeSchema))]
public sealed class Trade { }

public sealed class CustomTradeSchema : IDataGridGeneratedSchema<Trade>
{
    // Implement the focused provider/compiler contracts.
}
```

The emitted `TradeSchema` remains the public stable facade and forwards to the implementation. This keeps consumers insulated from application implementation type names. Contract mismatches report `PDGSG007`.

## Controller factories

`GenerateDataGridController.ImplementationType` must implement `IDataGridGeneratedControllerFactory<TItem>`. Use this when creation depends on an application service abstraction.

For option-only policy, use a static method:

```csharp
[GenerateDataGridController(
    typeof(Trade),
    "Trades",
    ConfigureMethod = nameof(ConfigureTrades))]
public sealed partial class TradesViewModel
{
    private static void ConfigureTrades(
        ref DataGridGeneratedControllerOptions<Trade> options)
    {
        options.OperationExecution = DataGridOperationExecution.ExternalPipeline;
    }
}
```

## Assembly and namespace defaults

```csharp
[assembly: GenerateDataGridColumnsForNamespace(
    "MyApp.Models",
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true,
    Streaming = true)]

[assembly: GenerateDataGridViewModelsForNamespace(
    "MyApp.ViewModels")]

[assembly: GenerateDataGridViewsForNamespace(
    "MyApp.ViewModels",
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.SearchableGrid)]
```

`IncludeNestedNamespaces` controls recursive matching. Namespace policies are defaults; explicit type/assembly requests replace options they own. Direct property metadata remains most specific.

Compilation-wide policies are intentionally opt-in. Direct attributes use isolated incremental pipelines and do not enumerate unrelated source types.

## Generate a reflection-free registry

```csharp
[assembly: GenerateDataGridRegistry(
    RegistryName = "ApplicationGridRegistry",
    RegistryNamespace = "MyApp.Generated")]
```

The registry exposes all generated manifest providers and supports lookup by:

- item `Type`;
- stable schema ID;
- registered ViewModel type/view construction.

```csharp
if (ApplicationGridRegistry.TryGetSchema(
    typeof(Trade),
    out IDataGridGeneratedSchemaManifestProvider? schema))
{
    // No Type.GetType or Activator.CreateInstance.
}
```

Registry cases are emitted as direct typed references and deterministic type switches.

## Register existing XAML views

```csharp
[assembly: DataGridViewRegistration(
    typeof(TradesViewModel),
    typeof(TradesView))]

if (ApplicationGridRegistry.TryCreateView(
    viewModel,
    out Control? view))
{
    return view;
}
```

The view must derive from `Control` and have an accessible parameterless constructor. One ViewModel may have at most one registry mapping. The generated switch constructs the view and assigns `DataContext` directly.

This is suitable for reflection-free XAML view lookup when the view itself remains handwritten XAML.

## Optional Microsoft DI integration

When `Microsoft.Extensions.DependencyInjection` is referenced, the generated registry also emits:

```csharp
services.AddGeneratedProDataGrids();
```

Microsoft DI is optional. The generator emits no DI dependency when the abstractions are absent. Application-owned mutation handlers, query providers, interaction handlers, and other services should still be registered in the application composition root.

## Custom generated-view bases

```csharp
public abstract class ApplicationGridViewBase<TViewModel> :
    ReactiveUserControl<TViewModel>,
    IActivatableView
    where TViewModel : class
{
    protected ApplicationGridViewBase()
    {
        Activator = new ViewModelActivator();
    }

    public ViewModelActivator Activator { get; }
}
```

Apply the base with `BaseType`. It must be accessible, non-sealed, constructible, and compatible with the selected framework. ReactiveUI activation-scoped features require `IActivatableView`; violations report `PDGSG013`.

## Protected view factories

Generated views expose general layout hooks plus feature-specific factories. Common examples:

```csharp
protected virtual DataGrid CreateGeneratedDataGrid();
protected virtual void ConfigureGeneratedDataGrid(DataGrid dataGrid);
protected virtual Control? CreateGeneratedToolbar();
protected virtual Control? CreateGeneratedRecipeContent();
protected virtual IDataGridEditingInteractionModelFactory
    CreateGeneratedEditingInteractionModelFactory();
protected virtual IDataGridFilteringAdapterFactory
    CreateGeneratedHierarchicalFilteringAdapterFactory();
protected virtual IDataGridGeneratedInputMap CreateGeneratedInputMap();
protected virtual IDataGridGeneratedMetricsSink CreateGeneratedMetricsSink();
```

Interaction and navigation handlers receive their own protected factories. Override these factories in a derived generated view to resolve scoped services while retaining compile-time contracts and generated disposal.

## Customization precedence

The effective precedence is:

1. Explicit implementation/factory type or view override.
2. Validated static factory/configure method.
3. Generated default.
4. Explicitly permitted compatibility fallback.

An explicit replacement owns the behavior it replaces. For example, a custom column factory owns the initial binding, and a custom interaction handler owns the response semantics.

## Preserve the MVVM boundary

Customization of visual composition belongs in view bases/derived views. Commands, state transitions, query logic, validation, and domain mutation belong in ViewModels or services. Generated views should not become a service locator.

## Production example

`GeneratedCustomImplementationsPage` combines:

- a custom compiled column factory;
- schema-list policy;
- domain comparer and validator;
- custom summary calculator;
- ReactiveUI base class;
- derived generated page with toolbar/content overrides;
- compiled custom command/status bindings.

The ProDiagnostics migration uses generated registries and explicit XAML view registrations across multiple assemblies without runtime view-location reflection.
