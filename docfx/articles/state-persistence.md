# State and Persistence

ProDataGrid exposes two state APIs:

- Runtime state API (`CaptureState`, `RestoreState`, section-level capture/restore).
- Persisted state API (`DataGridStatePersistence`) for file-storable payloads (`string` and `byte[]`).

Use runtime API for in-memory snapshots during a session. Use persisted API when saving state outside the process.

## Runtime state API (in-memory)

```csharp
using System.Linq;

var options = new DataGridStateOptions
{
    ItemKeySelector = item => (item as MyRow)?.Id,
    ItemKeyResolver = key => Items.FirstOrDefault(row => Equals(row.Id, key)),
    ColumnKeySelector = column => column.Header?.ToString(),
    ColumnKeyResolver = key => grid.Columns.FirstOrDefault(column => Equals(column.Header, key))
};

var runtimeState = grid.CaptureState(DataGridStateSections.All, options);
grid.RestoreState(runtimeState, DataGridStateSections.All, options);
```

Section helpers remain available:

- `CaptureColumnLayoutState` / `RestoreColumnLayoutState`
- `CaptureSortingState` / `RestoreSortingState`
- `CaptureFilteringState` / `RestoreFilteringState`
- `CaptureSearchState` / `RestoreSearchState`
- `CaptureGroupingState` / `RestoreGroupingState`
- `CaptureHierarchicalState` / `RestoreHierarchicalState`
- `CaptureSelectionState` / `RestoreSelectionState`
- `CaptureScrollState` / `TryRestoreScrollState`

## Persisted state API (string/byte[])

`DataGridStatePersistence` maps runtime state to `DataGridPersistedState`, then serializes with built-in `System.Text.Json` by default.

### JSON string workflow

```csharp
var payload = DataGridStatePersistence.SerializeStateToString(
    grid,
    DataGridStateSections.All,
    options);

DataGridStatePersistence.RestoreStateFromString(
    grid,
    payload,
    DataGridStateSections.All,
    options);
```

### Binary + Base64 workflow

```csharp
var bytes = DataGridStatePersistence.SerializeState(
    grid,
    DataGridStateSections.All,
    options);

var base64 = DataGridStatePersistence.EncodeBase64(bytes);

var restoredBytes = DataGridStatePersistence.DecodeBase64(base64);
DataGridStatePersistence.RestoreState(
    grid,
    restoredBytes,
    DataGridStateSections.All,
    options);
```

The `DataGridSample` app includes this workflow in the **State - Full** tab.

## Pluggable serializer

Implement `IDataGridStateSerializer` to use another format:

```csharp
public sealed class MySerializer : IDataGridStateSerializer
{
    public string FormatId => "my-format";

    public byte[] Serialize(DataGridPersistedState state) => ...;
    public DataGridPersistedState Deserialize(ReadOnlySpan<byte> payload) => ...;
    public string SerializeToString(DataGridPersistedState state) => ...;
    public DataGridPersistedState Deserialize(string payload) => ...;
}
```

Pass the serializer to `Serialize`, `SerializeToString`, `Deserialize`, `SerializeState`, or `RestoreState`.

## Unsupported runtime members and behavior

Some runtime members are not directly serializable (for example delegates and comparer/theme/converter instances).

Configure behavior with `DataGridStatePersistenceOptions.UnsupportedBehavior`:

- `Throw` (default): fail fast with `DataGridStatePersistenceException`.
- `Skip`: ignore unsupported entries and continue.

## Token-based runtime reconstruction (advanced)

For custom runtime-only members, use token contracts:

- `IDataGridStatePersistenceTokenProvider` during capture/serialization.
- `IDataGridStatePersistenceTokenResolver` during restore/deserialization.

These enable persistence for:

- Sorting comparer (`ComparerToken`)
- Filtering predicate (`PredicateToken`)
- Conditional formatting predicate/theme (`PredicateToken`, `ThemeToken`)
- Grouping value converter (`ValueConverterToken`)

Attach them via `DataGridStatePersistenceOptions`:

```csharp
var persistenceOptions = new DataGridStatePersistenceOptions
{
    TokenProvider = myProvider,
    TokenResolver = myResolver
};

var payload = DataGridStatePersistence.SerializeStateToString(
    grid,
    DataGridStateSections.All,
    options,
    serializer: null,
    persistenceOptions: persistenceOptions);
```

## Stability guidance

For durable payloads across app restarts and data refreshes:

- Always provide stable item and column keys in `DataGridStateOptions`.
- Prefer primitive keys/values (`string`, numeric, `bool`, `Guid`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `char`).
- Avoid using transient object references as keys.
- Use token provider/resolver for custom comparers, predicates, themes, and converters.
- Keep `DataGridPersistedState.Version` in persisted payloads for compatibility handling.

## Migration guidance

When moving from runtime state persistence to serialized persistence:

- Keep existing runtime calls for ephemeral in-session scenarios.
- Replace file/database persistence of runtime state objects with `DataGridStatePersistence`.
- Reuse existing `DataGridStateOptions` key selectors/resolvers unchanged.
- Add token provider/resolver only if you depend on custom runtime-only behavior.
