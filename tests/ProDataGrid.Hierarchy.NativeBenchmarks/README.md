# Native hierarchy source comparison

This harness compares ProDataGrid with Wieslaw's open-source TreeDataGrid using
source project references only. It has no paid-grid assembly or package reference.
The workflow pins the TreeDataGrid source revision so every process uses the same
implementation.

The two controls use the same generated models, five data columns, an 800 x 500
native desktop window, fixed 24-pixel rows, layout, and a two-animation-frame
completion wait. Every measured operation begins from a fully rendered state.

- `ExpandAllAndRender` expands the 4,094-node binary-tree workload used by the
  existing native expansion comparison.
- `CollapseAllAndRender` starts with the sample's 149,792-node workload fully
  expanded, retains materialized children for equivalent post-collapse semantics,
  collapses to 32 roots, updates layout, and waits for rendered completion.
- Managed allocation is `GC.GetTotalAllocatedBytes` traffic during the timed
  operation. It is not retained heap, native allocation, RSS, or GPU memory.
- Collapse results also split the same end-to-end sample into synchronous model/UI
  mutation, `UpdateLayout`, and rendered-frame wait durations. These diagnostic
  phase means sum to the reported collapse mean; the end-to-end mean remains the
  comparison and gate metric.

The CI comparison runs four independent processes for each mode, alternating
product order. Every process performs two warmups and ten measurements. The report
aggregates the four process means and includes a Student-t 95% confidence interval.
Raw JSON and `aggregate.json` are uploaded as the
`hierarchy-native-source-windows` artifact.

Build both source applications locally:

```sh
dotnet build tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/Native.Pro.csproj -c Release
dotnet build tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Tree/Native.Tree.csproj -c Release \
  -p:TreeDataGridSourceRoot=/absolute/path/to/Avalonia.Controls.TreeDataGrid
```

Run one process per implementation:

```sh
GRID_BENCH_PRO_MODE=direct-cell dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --warmup 2 --iterations 10 --output /tmp/pro-direct-cell.json

dotnet \
  tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Tree/bin/Release/net8.0/Native.Tree.dll \
  --warmup 2 --iterations 10 --output /tmp/tree.json
```

On a machine without .NET 8, `DOTNET_ROLL_FORWARD=Major` can run the net8.0
applications on a newer installed runtime, but the report must record that change.
Use the same runtime and machine for both implementations.
