// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Controls;

namespace DataGridSample.Generated;

/// <summary>
/// Exposes the sample assembly's generated registry across its internal build boundary.
/// </summary>
public static class GeneratedSampleRegistryFacade
{
    /// <summary>Finds a generated schema by its compile-time item type.</summary>
    public static bool TryGetSchema(
        Type itemType,
        out IDataGridGeneratedSchemaManifestProvider schema) =>
        SampleGeneratedSchemas.TryGetSchema(itemType, out schema);

    /// <summary>Finds a generated schema by its stable schema identifier.</summary>
    public static bool TryGetSchema(
        string schemaId,
        out IDataGridGeneratedSchemaManifestProvider schema) =>
        SampleGeneratedSchemas.TryGetSchema(schemaId, out schema);

    /// <summary>Creates an explicitly registered Avalonia view without reflection.</summary>
    public static bool TryCreateView(object? viewModel, out Control? view) =>
        SampleGeneratedSchemas.TryCreateView(viewModel, out view);
}
