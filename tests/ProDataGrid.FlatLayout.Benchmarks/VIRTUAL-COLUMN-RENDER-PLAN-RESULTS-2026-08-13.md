# Virtual column render-plan results — 2026-08-13

## Scope

The rowless virtual lane had already removed row/cell realization and reduced
`ScrollSlotsByHeight` by 74.2%. On the current baseline, a representative
discontinuous diagnostic process spent about 0.028 ms per jump in
`ScrollSlotsByHeight`, but about 0.278 ms recording the virtual surface. The next
owner was therefore the 20-row × 5-column render loop, not row recycling or frame
wait.

The baseline is commit `a1b150b55ee3a64746c6344c7d50028bb3457aea`. The
candidate prepares one compact value-type render plan per visible column before
the row loop. Each plan contains the already resolved column geometry, renderer
kind, value provider/accessor, typography, alignment, culture, and brush cache-key
identity. The cell loop traverses the contiguous plan storage through a
`ReadOnlySpan<T>` and `ref readonly`; it no longer repeats column type switches,
attached-property accessor lookup, typography resolution, culture lookup, or brush
identity extraction for every visible cell. Existing column formatters still own
value semantics.

This follows the useful direction of a flat canvas renderer: prepare stable state
once, then draw many cells. It does not add a second column model, retain per-cell
objects, bypass formatting, or change editing fallback.

## Environment and protocol

- macOS 26.6 (25G72), Arm64;
- Apple M3 Pro, 11 cores, 18 GB;
- .NET SDK 10.0.201; benchmark runtime .NET 10.0.5 running the net8.0 app with
  `DOTNET_ROLL_FORWARD=Major`;
- Avalonia 12.1.0, Skia, 800 × 500 window, render scale 2;
- `DOTNET_TieredCompilation=0`;
- `GRID_BENCH_PRO_MODE=virtual`;
- 4,094 expanded hierarchy rows, five fixed-width columns, 20 rendered rows and
  100 rendered cells per pass;
- 32 deterministic scroll operations per iteration;
- discontinuous, line, and fractional scroll patterns;
- three warmups and ten measured iterations per diagnostic process;
- three alternating process pairs per pattern (`B C`, `C B`, `B C`); and
- separate meter-free processes with three warmups and 15 measured iterations.

Raw JSON is gitignored under
`artifacts/performance/virtual-render-plan-2026-08-13/`. Every process validated
4,094 logical rows, zero retained rows, zero retained cells, 62 visuals, and 62
controls.

## Active-work definition

Active work is scroll mutation + explicit layout + Avalonia UI render recording +
compositor update + compositor render. Frame wait is excluded: it is the harness's
asynchronous animation-frame barrier and includes refresh-clock/compositor pacing,
not CPU work owned by the DataGrid. Full wall, frame wait, and p95 wall remain
meter-free guardrails.

The ProDataGrid surface timer is nested attribution inside UI render recording and
is therefore not added again to Active.

## Diagnostic results

Values below are the mean of the three process means. The change column is the
median of the three paired percentage changes, so one favorable process does not
determine the result.

| Pattern | Surface baseline | Surface candidate | Median paired change | Active baseline | Active candidate | Median paired change |
|---|---:|---:|---:|---:|---:|---:|
| Discontinuous | 0.39458 ms | **0.29679 ms** | **−24.2%** | 1.21041 ms | **0.98506 ms** | **−17.8%** |
| Line | 0.29807 ms | **0.29090 ms** | **−3.0%** | 1.12874 ms | 1.12490 ms | −1.1% |
| Fractional | 0.14307 ms | **0.10848 ms** | **−22.3%** | 0.84740 ms | **0.73726 ms** | −1.2% |

The directly owned surface stage improved in every process pair:

| Pattern | Pair 1 | Pair 2 | Pair 3 |
|---|---:|---:|---:|
| Discontinuous | −24.2% | −35.0% | −14.8% |
| Line | −3.0% | −3.1% | −1.0% |
| Fractional | −22.3% | −6.4% | −43.0% |

The complete active-work pair changes were:

| Pattern | Pair 1 | Pair 2 | Pair 3 |
|---|---:|---:|---:|
| Discontinuous | −12.8% | −25.7% | −17.8% |
| Line | −1.5% | +1.4% | −1.1% |
| Fractional | −1.2% | −1.1% | −34.5% |

Discontinuous scrolling is the product-significant aggregate win. Line scrolling
already reuses 19 of 20 projected items, and its remaining plan delta is small.
Fractional UI surface recording improves, but compositor variability means the
conservative median Active result is only −1.2%; this is an owner-local improvement,
not a universal frame reduction.

`ScrollSlotsByHeight` is not the target of this change. Its three-pair median was
−20.0% for discontinuous, approximately unchanged for line, and −19.3% for
fractional, but the absolute operation remains only about 0.020–0.030 ms. Its
movement is correlated process variance rather than work removed by the render
plan.

## Meter-free guardrail

The clean lane enabled neither Avalonia nor ProDataGrid meters. Median paired
changes were:

| Pattern | Mutation + layout | Allocation | Full wall | Frame wait | p95 wall |
|---|---:|---:|---:|---:|---:|
| Discontinuous | +5.7% | +0.01% | +0.75% | +0.65% | −0.59% |
| Line | +14.0% | −0.02% | −0.44% | −0.89% | −7.59% |
| Fractional | −12.1% | −0.10% | +0.31% | +0.66% | +9.55% |

Mutation plus layout does not contain the changed render-recording work and moved
in both directions. Allocation is effectively unchanged. Full wall remains in the
same refresh-paced band. Fractional p95 was noisy across pairs (+9.6%, −0.9%,
+11.2%) while its wall means differed by at most 0.34%; it is a residual timing
limitation rather than evidence against the directly measured surface result.

## Correctness and renderer coverage

- 41 focused virtual/flat-layout and text-render-data tests passed.
- Native smoke processes passed for `virtual`, `virtual-checkbox`, `virtual-date`,
  `virtual-time`, `virtual-masked`, `virtual-autocomplete`,
  `virtual-slider-text`, and `virtual-combobox-text`.
- Every native smoke process kept zero retained rows and zero retained cells.
- Editing, automation, custom themes, unsupported interactive columns, auto cell
  sizing, pointer hit testing, frozen clips, selection, and value-change tracking
  retain their existing tests and fallback contracts.

## Rejected representations

Two implementations were measured and removed before the accepted plan:

- Copying a large plan struct through the 100-cell loop improved line scrolling
  but regressed all three discontinuous diagnostic pairs.
- Reusing five reference-type plan objects removed the copies but introduced
  object indirection and produced inconsistent results across patterns.

The accepted form keeps contiguous value storage, uses `ref readonly` iteration,
and passes plans by `in`. This preserves the preparation benefit without either
large-struct copies or per-cell object indirection.

## Commands

Representative diagnostic command:

```sh
DOTNET_ROLL_FORWARD=Major \
DOTNET_TieredCompilation=0 \
GRID_BENCH_PRO_MODE=virtual \
dotnet tests/ProDataGrid.Hierarchy.NativeBenchmarks/Native.Pro/bin/Release/net8.0/Native.Pro.dll \
  --scroll-only --scroll-pattern line --scroll-jumps 32 \
  --warmup 3 --iterations 10 \
  --avalonia-diagnostics --prodatagrid-diagnostics \
  --output artifacts/performance/virtual-render-plan-2026-08-13/line.json
```

The meter-free lane removes both diagnostic switches and uses 15 measured
iterations. Baseline and candidate were built in separate worktrees and executed
as separate native processes on the same machine.
