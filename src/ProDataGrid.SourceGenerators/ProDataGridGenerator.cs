// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        IncrementalValuesProvider<CellDrawCacheCandidate> cellDrawCacheCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateCellDrawCacheAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateCellDrawCacheCandidate(attributeContext))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(CellDrawCacheCandidateComparer.Instance)
            .WithTrackingName("CellDrawCacheCandidates");
        IncrementalValuesProvider<CellDrawCacheGenerationResult> cellDrawCacheResults = cellDrawCacheCandidates
            .Select(static (candidate, cancellationToken) => Discovery.BuildCellDrawCache(candidate, cancellationToken))
            .WithTrackingName("CellDrawCacheGeneration");
        context.RegisterSourceOutput(
            cellDrawCacheResults.SelectMany(static (result, _) => result.Diagnostics),
            static (productionContext, diagnostic) => productionContext.ReportDiagnostic(diagnostic));
        context.RegisterSourceOutput(
            cellDrawCacheResults.Where(static result => result.Source != null).Select(static (result, _) => result.Source!.Value),
            static (productionContext, source) => productionContext.AddSource(source.HintName, source.Source));

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

        IncrementalValuesProvider<DirectSchemaCandidate> directPropertySchemaCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ColumnAttributeName,
                static (node, _) => node is PropertyDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateDirectPropertySchemaCandidate(attributeContext))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(DirectSchemaCandidateComparer.Instance)
            .WithTrackingName("DirectPropertySchemaCandidates");

        IncrementalValuesProvider<DirectSchemaCandidate> directViewModelSchemaCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateViewModelAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateDirectOwnerSchemaCandidate(
                    attributeContext,
                    DirectSchemaSourceKind.ViewModel))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(DirectSchemaCandidateComparer.Instance)
            .WithTrackingName("DirectViewModelSchemaCandidates");

        IncrementalValuesProvider<DirectSchemaCandidate> directControllerSchemaCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateControllerAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateDirectOwnerSchemaCandidate(
                    attributeContext,
                    DirectSchemaSourceKind.Controller))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(DirectSchemaCandidateComparer.Instance)
            .WithTrackingName("DirectControllerSchemaCandidates");

        IncrementalValueProvider<DirectSchemaCompositionResult> directSchemaComposition = directSchemaCandidates
            .Collect()
            .Combine(directPropertySchemaCandidates.Collect())
            .Combine(directViewModelSchemaCandidates.Collect())
            .Combine(directControllerSchemaCandidates.Collect())
            .Select(static (input, cancellationToken) => Discovery.ComposeDirectSchemas(
                input.Left.Left.Left,
                input.Left.Left.Right,
                input.Left.Right,
                input.Right,
                cancellationToken))
            .WithTrackingName("DirectSchemaComposition");

        context.RegisterSourceOutput(
            directSchemaComposition.SelectMany(static (result, _) => result.Diagnostics),
            static (productionContext, diagnostic) => productionContext.ReportDiagnostic(diagnostic));

        IncrementalValuesProvider<DirectSchemaBuildCandidate> directSchemaBuildCandidates = directSchemaComposition
            .SelectMany(static (result, _) => result.Schemas)
            .WithComparer(DirectSchemaBuildCandidateComparer.Instance)
            .WithTrackingName("DirectSchemaBuildCandidates");

        IncrementalValuesProvider<DirectSchemaGenerationResult> directSchemas = directSchemaBuildCandidates
            .Select(static (candidate, cancellationToken) =>
                Discovery.BuildDirectSchema(candidate, cancellationToken))
            .WithTrackingName("DirectSchemaGeneration");

        IncrementalValueProvider<(ImmutableArray<DirectSchemaGenerationResult> Results, DirectSchemaCompositionResult Composition)>
            directSchemaOutputs = directSchemas
                .Collect()
                .Combine(directSchemaComposition);
        context.RegisterSourceOutput(directSchemaOutputs, static (productionContext, output) =>
        {
            var currentCacheKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (DirectSchemaBuildCandidate candidate in output.Composition.Schemas)
            {
                currentCacheKeys.Add(candidate.CacheKey);
            }

            var currentSources = new Dictionary<string, GeneratedSource>(StringComparer.Ordinal);
            var processedCacheKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (DirectSchemaGenerationResult result in output.Results)
            {
                if (!currentCacheKeys.Contains(result.CacheKey) ||
                    !processedCacheKeys.Add(result.CacheKey))
                {
                    continue;
                }

                foreach (Diagnostic diagnostic in result.Diagnostics)
                {
                    productionContext.ReportDiagnostic(diagnostic);
                }

                foreach (GeneratedSource source in result.Sources)
                {
                    currentSources[source.HintName] = source;
                }
            }

            var orderedHintNames = new List<string>(currentSources.Keys);
            orderedHintNames.Sort(StringComparer.Ordinal);
            foreach (string hintName in orderedHintNames)
            {
                productionContext.AddSource(hintName, currentSources[hintName].Source);
            }
        });

        IncrementalValuesProvider<DirectViewCandidate> directViewCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateViewAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateDirectViewCandidate(attributeContext))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(DirectViewCandidateComparer.Instance)
            .WithTrackingName("DirectViewCandidates");

        IncrementalValueProvider<DirectViewGenerationResult> directViews = directViewCandidates
            .Collect()
            .Select(static (candidates, cancellationToken) =>
                Discovery.BuildDirectViews(candidates, cancellationToken))
            .WithTrackingName("DirectViewComposition");

        context.RegisterSourceOutput(
            directViews.SelectMany(static (result, _) => result.Diagnostics),
            static (productionContext, diagnostic) => productionContext.ReportDiagnostic(diagnostic));

        IncrementalValuesProvider<GeneratedSource> directViewSources = directViews
            .SelectMany(static (result, _) => result.Sources)
            .WithComparer(GeneratedSourceComparer.Instance)
            .WithTrackingName("DirectViewSources");
        context.RegisterSourceOutput(directViewSources, static (productionContext, source) =>
            productionContext.AddSource(source.HintName, source.Source));

        IncrementalValuesProvider<DirectViewModelCandidate> directViewModelCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateViewModelAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateDirectViewModelCandidate(attributeContext))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(DirectViewModelCandidateComparer.Instance)
            .WithTrackingName("DirectViewModelCandidates");

        IncrementalValueProvider<DirectViewModelGenerationResult> directViewModels = directViewModelCandidates
            .Collect()
            .Select(static (candidates, cancellationToken) =>
                Discovery.BuildDirectViewModels(candidates, cancellationToken))
            .WithTrackingName("DirectViewModelComposition");

        context.RegisterSourceOutput(
            directViewModels.SelectMany(static (result, _) => result.Diagnostics),
            static (productionContext, diagnostic) => productionContext.ReportDiagnostic(diagnostic));

        IncrementalValuesProvider<GeneratedSource> directViewModelSources = directViewModels
            .SelectMany(static (result, _) => result.Sources)
            .WithComparer(GeneratedSourceComparer.Instance)
            .WithTrackingName("DirectViewModelSources");
        context.RegisterSourceOutput(directViewModelSources, static (productionContext, source) =>
            productionContext.AddSource(source.HintName, source.Source));

        IncrementalValuesProvider<DirectControllerCandidate> directControllerCandidates = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateControllerAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, _) => Discovery.CreateDirectControllerCandidate(attributeContext))
            .Where(static candidate => candidate != null)
            .Select(static (candidate, _) => candidate!)
            .WithComparer(DirectControllerCandidateComparer.Instance)
            .WithTrackingName("DirectControllerCandidates");

        IncrementalValueProvider<DirectControllerGenerationResult> directControllers = directControllerCandidates
            .Collect()
            .Select(static (candidates, cancellationToken) =>
                Discovery.BuildDirectControllers(candidates, cancellationToken))
            .WithTrackingName("DirectControllerComposition");

        context.RegisterSourceOutput(
            directControllers.SelectMany(static (result, _) => result.Diagnostics),
            static (productionContext, diagnostic) => productionContext.ReportDiagnostic(diagnostic));

        IncrementalValuesProvider<GeneratedSource> directControllerSources = directControllers
            .SelectMany(static (result, _) => result.Sources)
            .WithComparer(GeneratedSourceComparer.Instance)
            .WithTrackingName("DirectControllerSources");
        context.RegisterSourceOutput(directControllerSources, static (productionContext, source) =>
            productionContext.AddSource(source.HintName, source.Source));

        IncrementalValuesProvider<Compilation> compilationWideRequests = context.CompilationProvider
            .Select(static (compilation, _) =>
                Discovery.HasCompilationWideRequests(compilation.Assembly.GetAttributes())
                    ? ImmutableArray.Create(compilation)
                    : ImmutableArray<Compilation>.Empty)
            .SelectMany(static (compilations, _) => compilations)
            .WithTrackingName("CompilationWideRequests");

        IncrementalValuesProvider<GenerationModel> model = compilationWideRequests
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
