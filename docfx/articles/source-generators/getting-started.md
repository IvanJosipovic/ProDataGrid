# Getting started and schema discovery

The source generator is distributed as `ProDataGrid.SourceGenerators`. It injects configuration attributes into the consuming compilation and emits code that depends on the normal `ProDataGrid` runtime package. There is no runtime attributes assembly to deploy.

## Install

```xml
<ItemGroup>
  <PackageReference Include="ProDataGrid" />
  <PackageReference Include="ProDataGrid.SourceGenerators"
                    PrivateAssets="all" />
</ItemGroup>
```

When referencing the generator project directly, consume it as an analyzer:

```xml
<ProjectReference Include="..\ProDataGrid.SourceGenerators\ProDataGrid.SourceGenerators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

The generator is incremental. Direct schema, ViewModel, controller, view, indexed-column, and cell-cache requests are discovered independently. Compilation-wide type enumeration is enabled only by assembly/namespace policies or the generated registry.

## Generate a first schema

```csharp
using ProDataGrid.SourceGeneration;

[GenerateDataGridColumns(
    ProviderName = "OrderGridSchema",
    SchemaId = "orders/order/v1",
    Strict = true)]
public sealed class Order
{
    [DataGridKey]
    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "Order", ColumnKey = "order-id", Order = 0,
        IsReadOnly = true)]
    public long Id { get; init; }

    [DataGridColumn(DataGridColumnKind.Text,
        Header = "Customer", ColumnKey = "customer", Order = 1,
        Width = "2*")]
    public string Customer { get; set; } = string.Empty;

    [DataGridColumn(DataGridColumnKind.Numeric,
        Header = "Total", ColumnKey = "total", Order = 2,
        FormatString = "C2")]
    public decimal Total { get; set; }
}
```

The default `PublicProperties` discovery mode includes eligible public instance properties. Choose `AttributedOnly` for explicit opt-in:

```csharp
[GenerateDataGridColumns(
    Discovery = DataGridColumnDiscovery.AttributedOnly)]
public sealed class AuditRow
{
    [DataGridColumn(Header = "Event")]
    public string Event { get; init; } = string.Empty;

    public object InternalPayload { get; init; } = new();
}
```

`[DataGridIgnoreColumn]` excludes one otherwise eligible public property. `IncludeInherited = false` restricts discovery to properties declared directly on the annotated type.

## Use the generated provider directly

The provider name defaults to a deterministic type-based name. Set `ProviderName` when application code, persisted configuration, or another assembly needs a stable source identifier.

```csharp
DataGridColumnDefinitionList columns = OrderGridSchema.CreateColumnDefinitions();
DataGridFastPathOptions fastPath = OrderGridSchema.CreateFastPathOptions();

SortingDescriptor[] sorting =
[
    OrderGridSchema.Total.Descending(),
    OrderGridSchema.Customer.Ascending()
];
```

Every call to `CreateColumnDefinitions` returns a fresh mutable list. The provider and its field descriptors are immutable reusable metadata.

## Augment an existing ViewModel

The target must be partial. It may inherit any framework-required base class, including `ReactiveObject`.

```csharp
[GenerateDataGridViewModel(
    typeof(Order),
    ProviderName = "OrderGridSchema")]
public sealed partial class OrdersViewModel : ReactiveObject
{
    public IReadOnlyList<Order> Items { get; } = LoadOrders();
}
```

The generated members are:

```csharp
public IDataGridGeneratedSchema<Order> DataGridSchema { get; }
public DataGridColumnDefinitionList ColumnDefinitions { get; }
public DataGridFastPathOptions FastPathOptions { get; }
```

All names can be changed with `SchemaPropertyName`, `ColumnDefinitionsPropertyName`, and `FastPathOptionsPropertyName`. Multiple projections on one partial type are supported when every generated member name is distinct.

Bind with compiled XAML:

```xml
<DataGrid ItemsSource="{Binding Items}"
          ColumnDefinitionsSource="{Binding ColumnDefinitions}"
          FastPathOptions="{Binding FastPathOptions}"
          AutoGenerateColumns="False" />
```

`FastPathOptions` is a direct Avalonia property, so no attached behavior or view code is necessary.

## Class, struct, and interface contracts

Schemas can target classes, structs, and interfaces. Interface discovery walks inherited interfaces deterministically:

```csharp
public interface IEntity
{
    [DataGridKey]
    int Id { get; }
}

[GenerateDataGridColumns(ProviderName = "TradeContractSchema")]
public interface ITrade : IEntity
{
    string Symbol { get; set; }
    decimal Price { get; }
}
```

If unrelated parent interfaces declare the same property name, redeclare the property on the target interface to select the contract. Otherwise `PDGSG132` reports the ambiguity.

Class schemas support statically resolvable explicit-interface properties. The generated accessor uses a direct interface cast. A public property with the same name wins. Ambiguous unrelated explicit implementations produce `PDGSG133` rather than an unstable generated name.

## Generate for a model you cannot modify

Apply a type request at assembly level:

```csharp
using ProDataGrid.SourceGeneration;

[assembly: GenerateDataGridColumns(
    typeof(ExternalModels.Order),
    ProviderName = "ExternalOrderSchema",
    ProviderNamespace = "MyApp.Generated")]

[assembly: GenerateDataGridViewModel(
    typeof(OrdersViewModel),
    typeof(ExternalModels.Order),
    ProviderName = "ExternalOrderSchema")]
```

Assembly requests support the same strictness, discovery, schema, streaming, performance, paging, identity, and customization options as type requests where applicable.

## Namespace policies

Use namespace policies for a deliberate application-wide convention:

```csharp
[assembly: GenerateDataGridColumnsForNamespace(
    "MyApp.Models",
    IncludeNestedNamespaces = true,
    Discovery = DataGridColumnDiscovery.AttributedOnly,
    Strict = true)]

[assembly: GenerateDataGridViewModelsForNamespace(
    "MyApp.ViewModels",
    ItemsPropertyName = "Items")]
```

ViewModel namespace generation infers the item type from the configured items property. An absent or ambiguous item type reports `PDGSG011`.

Namespace policies establish defaults. Explicit assembly/type requests replace the settings they own. Property attributes remain the most specific column metadata.

Use a namespace policy only when matching future types is intended. Direct type requests have a smaller incremental invalidation surface.

## Strict mode

`Strict = true` is recommended for reflection-free applications. Strict generation reports unsupported shapes, inaccessible members, incomplete factories, and missing accessors at compile time. It does not silently fall back to property discovery.

Strict generation does not make arbitrary domain objects statically knowable. Dictionary rows, `DataRowView`, `IDataRecord`, `ICustomTypeDescriptor`, and dynamic meta-object shapes require an explicit runtime schema adapter; see [runtime-defined shapes](schemas-and-columns.md#runtime-defined-shapes).

## Inspect generated code

Enable compiler-generated file output in the consuming project when diagnosing build behavior:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated hint names are stable and include the provider or generated member owner. Do not compile the output directory as ordinary source; it is diagnostic output from the analyzer pipeline.

## Next steps

- [Schemas, columns, accessors, and manifests](schemas-and-columns.md)
- [Operations and controllers](operations-and-controllers.md)
- [Generate code-only views](generated-views.md)
- [Attribute reference](attribute-reference.md)
