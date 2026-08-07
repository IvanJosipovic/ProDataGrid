// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ProDataGrid.SourceGenerators.UnitTests;

internal static class GeneratorTestHelper
{
    public static GeneratorTestResult Run(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            new[] { syntaxTree },
            GetReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        ISourceGenerator generator = new ProDataGridGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator },
            parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation updatedCompilation, out ImmutableArray<Diagnostic> driverDiagnostics);
        GeneratorDriverRunResult runResult = driver.GetRunResult();
        ImmutableArray<Diagnostic> generatorDiagnostics = runResult.Results.SelectMany(static result => result.Diagnostics).ToImmutableArray();
        ImmutableArray<Diagnostic> compilationDiagnostics = updatedCompilation.GetDiagnostics();
        string[] generatedSources = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .Select(static sourceResult => sourceResult.SourceText.ToString())
            .ToArray();

        return new GeneratorTestResult(
            generatedSources,
            generatorDiagnostics,
            driverDiagnostics,
            compilationDiagnostics);
    }

    private static ImmutableArray<MetadataReference> GetReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? trusted = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (!string.IsNullOrEmpty(trusted))
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
}

internal sealed class GeneratorTestResult
{
    public GeneratorTestResult(
        IReadOnlyList<string> sources,
        ImmutableArray<Diagnostic> generatorDiagnostics,
        ImmutableArray<Diagnostic> driverDiagnostics,
        ImmutableArray<Diagnostic> compilationDiagnostics)
    {
        Sources = sources;
        GeneratorDiagnostics = generatorDiagnostics;
        DriverDiagnostics = driverDiagnostics;
        CompilationDiagnostics = compilationDiagnostics;
    }

    public IReadOnlyList<string> Sources { get; }

    public ImmutableArray<Diagnostic> GeneratorDiagnostics { get; }

    public ImmutableArray<Diagnostic> DriverDiagnostics { get; }

    public ImmutableArray<Diagnostic> CompilationDiagnostics { get; }

    public string CombinedSource => string.Join("\n-----\n", Sources);

    public IEnumerable<Diagnostic> Errors => DriverDiagnostics
        .Concat(CompilationDiagnostics)
        .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
