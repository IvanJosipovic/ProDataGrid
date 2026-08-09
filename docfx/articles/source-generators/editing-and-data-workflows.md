# Editing and data workflows

Generated schemas expose typed edit fields and adapters for conversion, validation, undo/redo, clipboard import/export, fill, conditional formatting, and drag/drop. Domain mutation remains behind user-owned interfaces.

## Typed edit fields

For a keyed schema, writable non-read-only columns become `IDataGridGeneratedEditField<TItem>` values. Their accessors, parser/formatter, validation, coercion, and eligibility delegates are generated once.

```csharp
[GenerateDataGridColumns(ProviderName = "OrderSchema")]
public sealed class Order
{
    [DataGridKey]
    public int Id { get; init; }

    [DataGridColumn(
        DataGridColumnKind.Numeric,
        ColumnKey = "quantity",
        ParserMethod = nameof(ParseQuantity),
        ValidatorMethod = nameof(ValidateQuantity),
        CoerceMethod = nameof(CoerceQuantity),
        CanEditMethod = nameof(CanEditQuantity))]
    public int Quantity { get; set; }
}
```

Supported hook categories are:

- `ParserMethod` and `FormatterMethod`;
- `ValidatorMethod` and cancellable `AsyncValidatorMethod`;
- `CoerceMethod`;
- `CanEditMethod`.

DataAnnotations for required values, string length, min/max length, and numeric ranges compile into the same direct field validation. Invalid hook signatures report `PDGSG004`.

## Edit controller and validation projection

```csharp
DataGridGeneratedEditController<Order, int> edits =
    OrderSchema.CreateEditController(ResolveOrder);

DataGridGeneratedValidationProjection<Order, int> validation =
    OrderSchema.CreateValidationProjection(edits);

IDisposable subscription = validation.Subscribe(change =>
    OnValidationChanged(
        change.ItemKey,
        change.ColumnKey,
        change.Result));
```

Asynchronous validation is revisioned per item key and column key. A canceled or superseded result cannot overwrite a newer result.

The projection implements `INotifyDataErrorInfo`, direct keyed error lookup, and `IObservable<DataGridGeneratedValidationChange<TKey>>`. Successful edits clear the matching error.

## Undo and redo

The edit controller records typed changes by stable item and column key. Clipboard and fill operations are recorded as bounded batches rather than thousands of unrelated property operations.

Applications can supply a custom transaction/undo boundary while retaining generated parsing, validation, and field access.

## Clipboard import

```csharp
DataGridGeneratedClipboardImportModel<Order, int> clipboard =
    OrderSchema.CreateClipboardImportModel(
        edits,
        ReportTransfer,
        CultureInfo.InvariantCulture,
        new DataGridGeneratedTransferLimits(
            maximumCells: 10_000,
            maximumPayloadCharacters: 1_000_000));
```

The adapter maps runtime columns through stable `ColumnKey`, not property paths. It supports rectangular tabular paste and one-value multi-cell paste, returns keyed parse/validation errors, checks cancellation and bounds, and records one undo batch.

Generated export supports text, CSV, JSON, Markdown, HTML, XML, and YAML through the same typed field formatters. `ExportFormat`, `ExportNullText`, and `IsSensitive` customize per-field behavior.

## Fill and formula translation

```csharp
DataGridGeneratedFillModel<Order, int> fill =
    OrderSchema.CreateFillModel(
        edits,
        ReportTransfer,
        maximumCells: 10_000);
```

The generated fill model supports bounded cyclic copy plus numeric, date/time, and duration series.

Inject `IFormulaFillTranslator` when a string/formula field needs relative translation, or configure a validated default:

```csharp
[GenerateDataGridColumns(
    FormulaFillTranslatorType = typeof(ExcelFormulaFillTranslator))]
public sealed class FormulaRow
{
    [DataGridKey]
    public int Id { get; init; }

    [DataGridColumn(ColumnKey = "formula")]
    public string Formula { get; set; } = "=B1*C1";
}

DataGridGeneratedFillModel<FormulaRow, int> fill =
    FormulaRowDataGridSchema.CreateConfiguredFormulaFillModel(edits);
```

`ExcelFormulaFillTranslator` shifts relative A1 references, preserves absolute dimensions, R1C1 offsets, sheets, names, and structured references, and emits `#REF!` when copying crosses the A1 boundary. An invalid configured implementation reports `PDGSG137`.

## Bind edit workflows in a generated view

```csharp
[GenerateDataGridView(
    typeof(Order),
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.Spreadsheet,
    ClipboardImportModelPropertyName = nameof(ClipboardImportModel),
    FillModelPropertyName = nameof(FillModel),
    SelectionMode = DataGridSelectionMode.Extended,
    SelectionUnit = DataGridSelectionUnit.CellOrRowHeader,
    EditTriggers = DataGridEditTriggers.CellDoubleClick |
                   DataGridEditTriggers.TextInput |
                   DataGridEditTriggers.F2,
    RestrictTextInputEditToCells = true,
    ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader)]
public sealed partial class OrdersViewModel : ReactiveObject { }
```

Clipboard/fill model properties are compile-time validated (`PDGSG129`). Every generated view installs a `DataGridGeneratedEditingInteractionModelFactory` from its declared triggers and pointer-modifier policy. Override `CreateGeneratedEditingInteractionModelFactory` for a custom model.

Generated views disable add/delete rows by default to avoid entering a reflection-based new-item path. Set `CanUserAddRows` or `CanUserDeleteRows` only when a typed mutation/new-row service or equivalent application flow is installed.

## Conditional formatting

```csharp
[DataGridColumn(
    DataGridColumnKind.Numeric,
    Header = "Score",
    ColumnKey = "score")]
[DataGridConditionalFormat(
    DataGridCondition.GreaterThanOrEqual,
    RuleId = "high-score",
    Operand = "90",
    CellThemeKey = "HighScoreCellTheme",
    Priority = 100,
    StopIfTrue = true)]
public int Score { get; set; }
```

Generated rules support equality/ordering, inclusive ranges, null checks, ordinal text matching, and validated custom predicates. Each rule has a stable ID, field key, priority, stop behavior, resource key, and cell/row target.

Create the runtime model directly:

```csharp
IConditionalFormattingModel model =
    OrderSchema.CreateConditionalFormattingModel();
```

Or bind an application-owned model from a generated view with `ConditionalFormattingModelPropertyName`. Missing/incompatible members report `PDGSG131`.

Rules are immutable generated metadata; applications may enable, disable, replace, or merge them in the bound runtime model.

## Keyed drag/drop

Generated drag/drop adapters express source/target rows by stable key. `DataGridGeneratedDragDropController<TKey>` owns revisioning, cancellation, validation, and observable status; domain code owns authorization and mutation:

```csharp
public sealed class OrderDropHandler :
    IDataGridGeneratedDropHandler<int>
{
    public ValueTask ApplyAsync(
        DataGridGeneratedDropRequest<int> request,
        CancellationToken cancellationToken)
    {
        return request.Operation switch
        {
            DataGridGeneratedDropOperation.Move =>
                MoveAsync(request, cancellationToken),
            DataGridGeneratedDropOperation.Copy =>
                CopyAsync(request, cancellationToken),
            DataGridGeneratedDropOperation.Link =>
                LinkAsync(request, cancellationToken),
            _ => ValueTask.CompletedTask
        };
    }
}

DataGridGeneratedDragDropController<int> dragDrop = new(
    new OrderDropHandler(),
    validator: ValidateDropAsync);

bool applied = await dragDrop.DropAsync(
    selectedOrderIds,
    targetOrderId,
    DataGridGeneratedDropPosition.Before,
    DataGridGeneratedDropOperation.Move,
    cancellationToken);
```

The controller exposes `Idle`, `Validating`, `Rejected`, `Applying`, `Applied`, `Cancelled`, and `Failed` states. Hierarchical construction can also provide a descendant check to reject reparenting an item into its own subtree.

Flat and hierarchical adapters reuse the same schema key and bounded range mutation services. The generator does not decide business ordering, authorization, or cross-parent legality.

## Row commands and dynamic content

Button and toggle definitions can bind row command/content members through generated `ClrPropertyInfo` metadata:

```csharp
[DataGridColumn(
    DataGridColumnKind.Button,
    ContentMember = nameof(RestartLabel),
    CommandMember = nameof(RestartCommand),
    CommandParameterMember = nameof(Id))]
public string RestartAction => Id;
```

Supported options include `ContentMember`, checked/unchecked or on/off content members, `CommandMember`, and `CommandParameterMember`. Command members must implement `ICommand`; conflicts report `PDGSG124`.

When no parameter is configured, the row item is passed. A row command takes precedence over a definition-wide static command.

## Related articles

- [Editing interaction model](../editing-interaction-model.md)
- [Clipboard import model](../clipboard-import-model.md)
- [Fill handle and autofill](../fill-handle-and-autofill.md)
- [Conditional formatting](../conditional-formatting.md)
- [Drag/drop](../drag-drop.md)
