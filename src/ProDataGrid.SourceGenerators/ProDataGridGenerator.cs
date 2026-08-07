// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProDataGrid.SourceGenerators;

/// <summary>
/// Generates reflection-free ProDataGrid schemas from attributes and assembly conventions.
/// </summary>
[Generator]
public sealed partial class ProDataGridGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterAttributeSources(context);

        IncrementalValuesProvider<IndexedColumnsCandidate> indexedCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateIndexedColumnsAttributeName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateIndexedColumnsCandidates(attributeContext))
            .SelectMany(static (candidates, _) => candidates)
            .WithComparer(IndexedColumnsCandidateComparer.Instance)
            .WithTrackingName("IndexedColumnsCandidates");
        IncrementalValuesProvider<IndexedColumnsGenerationResult> indexedResults = indexedCandidates
            .Select(static (candidate, cancellationToken) => Discovery.BuildIndexedColumns(candidate, cancellationToken))
            .WithTrackingName("IndexedColumnsGeneration");
        context.RegisterSourceOutput(
            indexedResults.SelectMany(static (result, _) => result.Diagnostics),
            static (productionContext, diagnostic) => productionContext.ReportDiagnostic(diagnostic));
        context.RegisterSourceOutput(
            indexedResults.Where(static result => result.Source != null).Select(static (result, _) => result.Source!.Value),
            static (productionContext, source) => productionContext.AddSource(source.HintName, source.Source));

        IncrementalValuesProvider<DirectSchemaCandidate> directSchemaCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateColumnsAttributeName,
                static (node, _) => node is TypeDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateDirectSchemaCandidate(attributeContext))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(DirectSchemaCandidateComparer.Instance)
            .WithTrackingName("DirectSchemaCandidates");

        IncrementalValueProvider<DirectSchemaGenerationResult> directSchemas = directSchemaCandidates
            .Collect()
            .Select(static (candidates, cancellationToken) => Discovery.BuildDirectSchemas(candidates, cancellationToken))
            .WithTrackingName("DirectSchemaComposition");

        IncrementalValuesProvider<Diagnostic> directDiagnostics = directSchemas
            .SelectMany(static (result, _) => result.Diagnostics)
            .WithTrackingName("DirectSchemaDiagnostics");
        context.RegisterSourceOutput(directDiagnostics, static (productionContext, diagnostic) =>
            productionContext.ReportDiagnostic(diagnostic));

        IncrementalValuesProvider<GeneratedSource> directSources = directSchemas
            .SelectMany(static (result, _) => result.Sources)
            .WithComparer(GeneratedSourceComparer.Instance)
            .WithTrackingName("DirectSchemaSources");
        context.RegisterSourceOutput(directSources, static (productionContext, source) =>
            productionContext.AddSource(source.HintName, source.Source));

        IncrementalValueProvider<GenerationModel> model = context.CompilationProvider
            .Select(static (compilation, cancellationToken) => Discovery.Build(compilation, cancellationToken))
            .WithTrackingName("SemanticModel");

        IncrementalValuesProvider<Diagnostic> diagnostics = model
            .SelectMany(static (generationModel, _) => generationModel.Diagnostics)
            .WithTrackingName("Diagnostics");
        context.RegisterSourceOutput(diagnostics, static (productionContext, diagnostic) =>
            productionContext.ReportDiagnostic(diagnostic));

        IncrementalValuesProvider<GeneratedSource> sources = model
            .SelectMany(static (generationModel, cancellationToken) => Emitter.Emit(generationModel, cancellationToken))
            .WithComparer(GeneratedSourceComparer.Instance)
            .WithTrackingName("GeneratedSources");
        context.RegisterSourceOutput(sources, static (productionContext, source) =>
            productionContext.AddSource(source.HintName, source.Source));
    }
}
