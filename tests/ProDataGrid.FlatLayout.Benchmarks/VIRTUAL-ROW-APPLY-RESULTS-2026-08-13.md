# Virtual row retarget-apply results — 2026-08-13

This focused A/B targets the remaining synchronous work in discontinuous,
fixed-height virtual scrolling. New nested diagnostic phases first separate the
retarget bind into row-state application, logical child-index invalidation, and
post-bind layout-validity checking. The ownership trace attributed 0.07223 ms of
the 0.07463 ms bind phase (96.8%) to applying the new identity and state to the 20
retained rows; child-index invalidation was 0.00004 ms and the validity scan was
0.00044 ms.

The retained optimization caches the already-validated target slot in each typed
retarget entry and avoids invoking placeholder and validation property setters
when their backing state is already the required default. DataContext assignment,
row-index notification, selection, validation restoration, child-index reset, and
layout-validity checks remain intact.

## Environment and protocol

- Baseline: `1a0ad688` (`Batch virtual row diagnostics`) plus the new diagnostic
  subphase probes, before the target-slot and sparse-state changes.
- Candidate: this report's commit containing the target-slot and sparse-state
  changes.
- macOS 26.6, Arm64, .NET 10.0.5, Avalonia 12.1, Skia.
- Native window: 800 × 500 at 2×; 4,094 expanded hierarchy rows; fixed 24-pixel
  rows; five columns; 32 deterministic scroll jumps per iteration.
- Baseline and candidate binaries were copied after separate `Release` builds so
  every process executed an immutable binary.
- Every process used three warmups and eight measured iterations (256 jumps).
- The diagnostic lane used three alternating process pairs with both ProDataGrid
  and Avalonia diagnostics enabled.
- The clean lane used four ABBA process pairs with all diagnostic meters disabled.
- The component lane used four ABBA process pairs with Avalonia component timing
  enabled and ProDataGrid diagnostics disabled.
- `DOTNET_ROLL_FORWARD=Major` was required because the benchmark targets .NET 8
  and the machine had the .NET 10 runtime installed.

Raw JSON, logs, and the supporting sampled-thread trace remain under the gitignored
`artifacts/performance/virtual-row-work-2026-08-13` directory.

## Ownership and target-path result

Process-median component values were:

| Metric | Baseline | Candidate | Change |
|---|---:|---:|---:|
| Retarget apply | 0.07022 ms | **0.05718 ms** | **−18.6%** |
| Retarget bind | 0.07271 ms | **0.05932 ms** | **−18.4%** |
| Displayed-row update | 0.08867 ms | **0.07300 ms** | **−17.7%** |
| `ScrollSlotsByHeight` | 0.13163 ms | **0.10949 ms** | **−16.8%** |
| Diagnostic active work | 1.47643 ms | **1.37114 ms** | −7.1% |

The three diagnostic pairs changed retarget apply by −36.4%, +7.4%, and −23.2%.
This lane identifies ownership and direction, but it also exposes a faster machine
band in the reversed second pair. Acceptance therefore does not rely on the
instrumented percentage alone; the diagnostics-off synchronous-work lane is the
primary gate.

## Clean synchronous-work gate

| Pair | Mutation | Layout | Mutation + layout | Mean wall | Allocation per jump |
|---|---:|---:|---:|---:|---:|
| B1 → C1 | −7.2% | −5.4% | −6.1% | −0.1% | −0.10% |
| B2 → C2 | −12.3% | −12.4% | −12.3% | +0.3% | +0.00% |
| B3 → C3 | −9.7% | −12.4% | −11.3% | −0.3% | +0.01% |
| B4 → C4 | −28.9% | −32.0% | −30.7% | −0.7% | +0.11% |
| Process medians | **−11.0%** | **−12.4%** | **−11.8%** | **−0.2%** | +0.01% |

Mutation plus layout improved in all four pairs. Its process median moved from
0.27740 to 0.24462 ms. Mean wall moved from 8.24258 to 8.22400 ms and allocation
remained effectively flat, so the result is synchronous-work reduction rather
than a claim about the awaited frame interval.

## Active-component guardrail

Active work is mutation + explicit layout + UI render recording + compositor
update + compositor render. It excludes the awaited animation-clock interval.

| Metric | Baseline process median | Candidate process median | Change |
|---|---:|---:|---:|
| Active work | 0.92745 ms | **0.82971 ms** | **−10.5%** |
| Mean wall | **6.41990 ms** | 6.44821 ms | +0.4% |
| Frame wait | **6.24367 ms** | 6.28940 ms | +0.7% |
| Allocation per jump | 83,071 B | 83,069 B | −0.00% |

Three of four active-work pairs improved (−23.2%, +7.7%, −0.8%, and −21.6%).
The mixed magnitude tracks render-thread and machine-band noise; it does not
invalidate the all-pairs clean mutation-plus-layout result. The slightly longer
frame wait is idle refresh pacing and is deliberately excluded from Active.

## Architecture and correctness invariants

Every process retained:

- 20 realized and retargeted rows per discontinuous jump;
- zero realized display cells;
- 102 realized visuals;
- 20 reused row measures and 20 reused row arrangements; and
- the same transactional validate-before-mutate boundary.

Caching the target slot cannot make validation stale: the slot is resolved before
the corresponding row index and item and is stored in the same immutable entry.
Skipping an already-default direct-property setter preserves notifications because
`SetAndRaise` would also suppress a notification for an equal value. Invalid rows,
placeholder transitions, and `INotifyDataErrorInfo` items still execute the full
state-changing path. The focused flat-layout and diagnostics tests cover in-place
identity changes, selection, validation pseudo-classes, layout reuse, component
measurements, and routed-event fallback.

The remaining dominant retarget operation is the required DataContext/index
transition on each retained semantic row. A substantially larger discontinuous-
scroll gain would require a separate lightweight row-semantics/automation model,
not removal of the now-sub-microsecond child-index or layout-validity phases.
