# ProDataGrid source-generation benchmarks

This dedicated BenchmarkDotNet project measures two boundaries:

- generated runtime integration against an equivalent handwritten compiled fast path, with the expression-based compatibility path retained as an integration-cost reference;
- cold generation, incremental no-op reuse, and a single-schema semantic edit across representative compilation sizes.

Every runtime comparison uses the same five-column schema, stable keys, typed accessors, descriptors, rows, and result semantics. `HandwrittenCompiledFastPath` is the regression baseline. The expression benchmark includes expression construction and compilation because that work belongs to the compatibility integration path being replaced. It is not used as the fast-path baseline.

Run correctness guards before timing:

```bash
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --validate
```

List or run benchmarks:

```bash
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --list flat
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --anyCategories Runtime
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --anyCategories Generator
```

Use BenchmarkDotNet's short job only for smoke validation. Preserve the default-job artifacts for performance conclusions:

```bash
dotnet run -c Release --project tests/ProDataGrid.SourceGeneration.Benchmarks -- --filter '*ColumnDefinitionCreationBenchmarks*' --job short
```

## Reference measurement

The 2026-08-08 local reference run used BenchmarkDotNet 0.15.8, the default job, .NET SDK 10.0.201/.NET 10.0.5 Arm64 RyuJIT, macOS 26.6, and an 11-core Apple M3 Pro. BenchmarkDotNet could not elevate process priority, so treat these as local evidence rather than universal thresholds. The code under test was parent `28e84072` plus this benchmark/per-schema-isolation milestone while it was still uncommitted; only documentation was changed after the final run.

The five-column runtime shape produced these means; `±` is the reported standard deviation:

| Scenario | Handwritten compiled | Generated strict | Generated/handwritten allocation |
| --- | ---: | ---: | ---: |
| Create five definitions | 453.3 ± 2.94 ns, 4.41 KB | 448.1 ± 4.00 ns, 4.43 KB | 1.01x |
| Typed accessor, 32 rows | 113.6 ± 1.33 ns, 0 B | 113.5 ± 1.37 ns, 0 B | equal |
| Typed accessor, 4,096 rows | 15.181 ± 0.222 μs, 0 B | 15.523 ± 0.331 μs, 0 B | equal |
| Filter 4,096 rows | 47.47 ± 0.94 μs, 0 B | 46.52 ± 1.15 μs, 0 B | equal |
| Search 4,096 rows | 307.8 ± 5.60 μs, 1.13 MB | 308.4 ± 9.47 μs, 1.13 MB | 1.00x |
| Sort 4,096 rows | 19.08 ± 0.09 μs, 0 B | 19.56 ± 0.18 μs, 0 B | equal |

The expression-compatibility column-construction reference was 470.288 ± 14.968 μs and 100.88 KB; it is intentionally not the fast-path baseline. Generated typed accessors cross the object boundary only when explicitly requested: the 4,096-row object-boundary case was 23.534 ± 0.671 μs and allocated 128 KB.

The generator run used one schema per syntax tree so the edit case changes exactly one schema:

| Schemas | Cold | No-op | One-schema edit |
| ---: | ---: | ---: | ---: |
| 1 | 1.676 ± 0.067 ms, 995.9 KB | 0.678 ± 0.006 ms, 476.7 KB | 0.833 ± 0.008 ms, 685.8 KB |
| 32 | 6.985 ± 0.033 ms, 8.44 MB | 2.895 ± 0.011 ms, 2.11 MB | 3.547 ± 0.041 ms, 2.80 MB |

For 32 schemas, the isolated edit is 51% of cold-generation time and 33% of its managed allocation. The semantic-equivalence guard, generator compilation, and owner-option/provider-collision incremental tests must pass before comparing timing.

Results are written beneath `BenchmarkDotNet.Artifacts`. Record the commit and dirty state, `dotnet --info`, OS, architecture, hardware, power state, exact command, BenchmarkDotNet version, raw JSON/CSV/Markdown output, and run-to-run spread. Ordinary shared-runner PR CI builds the suite and runs correctness guards; it does not gate nanosecond timing. Timing trends belong on stable or dedicated hardware.
