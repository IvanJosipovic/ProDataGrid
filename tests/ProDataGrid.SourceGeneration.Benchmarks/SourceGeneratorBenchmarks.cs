// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Immutable;
using System.Text;
using Avalonia.Controls;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProDataGrid.SourceGenerators;

namespace ProDataGrid.SourceGeneration.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("Generator")]
public class SourceGeneratorBenchmarks
{
    private CSharpParseOptions _parseOptions = null!;
    private CSharpCompilation _baselineCompilation = null!;
    private CSharpCompilation _editedCompilation = null!;
    private GeneratorDriver _noOpDriver = null!;
    private GeneratorDriver _editDriver = null!;
    private bool _useEditedCompilation;

    [Params(1, 32)]
    public int SchemaCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var baselineTrees = new SyntaxTree[SchemaCount];
        for (int index = 0; index < SchemaCount; index++)
        {
            baselineTrees[index] = CSharpSyntaxTree.ParseText(
                CreateSource(index, edited: false),
                _parseOptions,
                $"Schema{index}.cs");
        }

        SyntaxTree editedTree = CSharpSyntaxTree.ParseText(
            CreateSource(0, edited: true),
            _parseOptions,
            "Schema0.cs");
        _baselineCompilation = CreateCompilation(baselineTrees);
        _editedCompilation = _baselineCompilation.ReplaceSyntaxTree(baselineTrees[0], editedTree);
        _noOpDriver = CreateDriver().RunGenerators(_baselineCompilation);
        _editDriver = CreateDriver().RunGenerators(_baselineCompilation);
    }

    [Benchmark(Baseline = true)]
    public int ColdGeneration()
    {
        GeneratorDriver driver = CreateDriver().RunGenerators(_baselineCompilation);
        return GetGeneratedCharacterCount(driver);
    }

    [Benchmark]
    public int IncrementalNoOp()
    {
        _noOpDriver = _noOpDriver.RunGenerators(_baselineCompilation);
        return GetGeneratedCharacterCount(_noOpDriver);
    }

    [Benchmark]
    public int IncrementalSingleTypeEdit()
    {
        CSharpCompilation compilation = _useEditedCompilation ? _baselineCompilation : _editedCompilation;
        _useEditedCompilation = !_useEditedCompilation;
        _editDriver = _editDriver.RunGenerators(compilation);
        return GetGeneratedCharacterCount(_editDriver);
    }

    internal void Validate()
    {
        Setup();
        int cold = ColdGeneration();
        int noOp = IncrementalNoOp();
        int edited = IncrementalSingleTypeEdit();
        if (cold <= 0 || noOp != cold || edited <= 0 || edited == cold)
        {
            throw new InvalidOperationException("The generator benchmark did not produce stable generated output.");
        }

        GeneratorDriverRunResult result = _editDriver.GetRunResult();
        if (result.Diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("The generator benchmark produced generator errors.");
        }

        Compilation outputCompilation = _editedCompilation;
        _editDriver.RunGeneratorsAndUpdateCompilation(_editedCompilation, out outputCompilation, out ImmutableArray<Diagnostic> diagnostics);
        if (diagnostics.Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) ||
            outputCompilation.GetDiagnostics().Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("The generator benchmark produced an invalid compilation.");
        }
    }

    private GeneratorDriver CreateDriver() => CSharpGeneratorDriver.Create(
        generators: [new ProDataGridGenerator().AsSourceGenerator()],
        parseOptions: _parseOptions);

    private static int GetGeneratedCharacterCount(GeneratorDriver driver)
    {
        int count = 0;
        foreach (GeneratorRunResult result in driver.GetRunResult().Results)
        {
            foreach (GeneratedSourceResult source in result.GeneratedSources)
            {
                count += source.SourceText.Length;
            }
        }
        return count;
    }

    private static CSharpCompilation CreateCompilation(IEnumerable<SyntaxTree> syntaxTrees) =>
        CSharpCompilation.Create(
            "SourceGenerationBenchmarks",
            syntaxTrees,
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
        {
            foreach (string path in trusted.Split(Path.PathSeparator))
            {
                paths.Add(path);
            }
        }

        paths.Add(typeof(DataGrid).Assembly.Location);
        paths.Add(typeof(Avalonia.AvaloniaObject).Assembly.Location);
        paths.Add(typeof(Avalonia.Data.Core.ClrPropertyInfo).Assembly.Location);
        return paths.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path)).ToImmutableArray();
    }

    private static string CreateSource(int index, bool edited)
    {
        var builder = new StringBuilder();
        builder.AppendLine("using ProDataGrid.SourceGeneration;")
            .AppendLine("namespace Benchmarks.Generated;");

        string header = edited ? "Changed" : "Value";
        builder.Append("[GenerateDataGridColumns(ProviderName = \"Schema").Append(index).AppendLine("\", Discovery = DataGridColumnDiscovery.AttributedOnly, Strict = true)]")
            .Append("public sealed class Row").Append(index).AppendLine()
            .AppendLine("{")
            .AppendLine("    [DataGridKey]")
            .AppendLine("    [DataGridColumn(DataGridColumnKind.Numeric, Header = \"ID\", ColumnKey = \"id\", Order = 0)]")
            .AppendLine("    public int Id { get; set; }")
            .Append("    [DataGridColumn(DataGridColumnKind.Text, Header = \"").Append(header).AppendLine("\", ColumnKey = \"value\", Order = 1)]")
            .AppendLine("    public string Value { get; set; } = string.Empty;")
            .AppendLine("}");
        return builder.ToString();
    }
}
