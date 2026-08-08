// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Avalonia.Controls;
using DataGridSample.Generated;
using DataGridSample.Models.SourceGenerationPolicy;
using DataGridSample.Models.SourceGenerationPolicy.Nested;
using DataGridSample.SourceGenerationPolicy.ViewModels;
using ReactiveUI;

namespace DataGridSample.ViewModels;

public sealed class GeneratedAssemblyNamespacePolicyPageViewModel : ReactiveObject
{
    public GeneratedAssemblyNamespacePolicyPageViewModel()
    {
        NamespacePolicy = new NamespacePolicyRowsViewModel();
        ExplicitPolicy = new ExplicitPolicyRowsViewModel();

        bool foundNamespace = GeneratedSampleRegistryFacade.TryGetSchema(
            typeof(NamespacePolicyRow),
            out IDataGridGeneratedSchemaManifestProvider namespaceSchema);
        bool foundExplicitByType = GeneratedSampleRegistryFacade.TryGetSchema(
            typeof(ExplicitPolicyRow),
            out IDataGridGeneratedSchemaManifestProvider explicitSchema);
        bool foundExplicitById = GeneratedSampleRegistryFacade.TryGetSchema(
            ExplicitPolicyRowSchema.SchemaId,
            out IDataGridGeneratedSchemaManifestProvider explicitSchemaById);
        bool foundNested = GeneratedSampleRegistryFacade.TryGetSchema(
            typeof(NestedPolicyRow),
            out _);

        NamespaceRegistrySchema = foundNamespace ? namespaceSchema : null;
        ExplicitRegistrySchema = foundExplicitByType && foundExplicitById && ReferenceEquals(explicitSchema, explicitSchemaById)
            ? explicitSchema
            : null;
        IsNestedNamespaceExcluded = !foundNested;

        NamespaceRegistryStatus = foundNamespace
            ? $"Namespace policy registered {nameof(NamespacePolicyRow)} by CLR type."
            : "Namespace policy schema is missing.";
        ExplicitRegistryStatus = foundExplicitByType && foundExplicitById && ReferenceEquals(explicitSchema, explicitSchemaById)
            ? $"Explicit override registered {explicitSchema.Manifest.SchemaId}."
            : "Explicit override lookup is inconsistent.";
        ExclusionStatus = IsNestedNamespaceExcluded
            ? "Nested namespace exclusion is active."
            : "Nested namespace was unexpectedly included.";
    }

    public NamespacePolicyRowsViewModel NamespacePolicy { get; }

    public ExplicitPolicyRowsViewModel ExplicitPolicy { get; }

    public IDataGridGeneratedSchemaManifestProvider? NamespaceRegistrySchema { get; }

    public IDataGridGeneratedSchemaManifestProvider? ExplicitRegistrySchema { get; }

    public bool IsNestedNamespaceExcluded { get; }

    public string NamespaceRegistryStatus { get; }

    public string ExplicitRegistryStatus { get; }

    public string ExclusionStatus { get; }

}
