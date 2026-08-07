// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ProDataGrid.SourceGenerators;

internal sealed class GenerationModel
{
    public GenerationModel(
        ImmutableArray<SchemaModel> schemas,
        ImmutableArray<ViewModelModel> viewModels,
        ImmutableArray<ViewModelViewModel> views,
        ImmutableArray<Diagnostic> diagnostics)
    {
        Schemas = schemas;
        ViewModels = viewModels;
        Views = views;
        Diagnostics = diagnostics;
    }

    public ImmutableArray<SchemaModel> Schemas { get; }

    public ImmutableArray<ViewModelModel> ViewModels { get; }

    public ImmutableArray<ViewModelViewModel> Views { get; }

    public ImmutableArray<Diagnostic> Diagnostics { get; }
}

internal enum ViewFrameworkModel
{
    Avalonia,
    ReactiveUI
}

internal sealed class ViewModelViewModel
{
    public INamedTypeSymbol ViewModelType { get; set; } = null!;

    public INamedTypeSymbol ItemType { get; set; } = null!;

    public string ViewName { get; set; } = string.Empty;

    public string ViewNamespace { get; set; } = string.Empty;

    public ViewFrameworkModel Framework { get; set; }

    public INamedTypeSymbol? BaseType { get; set; }

    public string Title { get; set; } = string.Empty;

    public ViewBindingModel Items { get; set; } = null!;

    public ViewBindingModel ColumnDefinitions { get; set; } = null!;

    public ViewBindingModel FastPathOptions { get; set; } = null!;

    public ViewBindingModel? SortingModel { get; set; }

    public ViewBindingModel? FilteringModel { get; set; }

    public ViewBindingModel? SearchModel { get; set; }

    public ViewBindingModel? SearchText { get; set; }

    public Location Location { get; set; } = Location.None;
}

internal sealed class ViewBindingModel
{
    public string PropertyName { get; set; } = string.Empty;

    public string PropertyType { get; set; } = "object";

    public string RuntimePropertyType { get; set; } = "object";

    public bool CanWrite { get; set; }
}

internal sealed class SchemaModel
{
    public INamedTypeSymbol ItemType { get; set; } = null!;

    public string ProviderName { get; set; } = string.Empty;

    public string ProviderNamespace { get; set; } = string.Empty;

    public bool AttributedOnly { get; set; }

    public bool IncludeInherited { get; set; } = true;

    public bool Strict { get; set; } = true;

    public bool Streaming { get; set; }

    public INamedTypeSymbol? ImplementationType { get; set; }

    public string? ConfigureMethod { get; set; }

    public Location Location { get; set; } = Location.None;

    public ImmutableArray<ColumnModel> Columns { get; set; } = ImmutableArray<ColumnModel>.Empty;
}

internal sealed class ColumnModel
{
    public IPropertySymbol Property { get; set; } = null!;

    public string Kind { get; set; } = "Auto";

    public string Header { get; set; } = string.Empty;

    public int Order { get; set; }

    public int SourceOrder { get; set; }

    public ImmutableDictionary<string, TypedConstant> Options { get; set; } = ImmutableDictionary<string, TypedConstant>.Empty;

    public string ColumnKey { get; set; } = string.Empty;

    public string? ConfigureMethod { get; set; }

    public string? FactoryMethod { get; set; }

    public bool IsSearchable { get; set; } = true;
}

internal sealed class ViewModelModel
{
    public INamedTypeSymbol ViewModelType { get; set; } = null!;

    public SchemaModel Schema { get; set; } = null!;

    public string ColumnDefinitionsPropertyName { get; set; } = "ColumnDefinitions";

    public string SchemaPropertyName { get; set; } = "DataGridSchema";

    public string FastPathOptionsPropertyName { get; set; } = "FastPathOptions";

    public bool GenerateColumnDefinitionsProperty { get; set; } = true;

    public bool GenerateSchemaProperty { get; set; } = true;

    public bool GenerateFastPathOptionsProperty { get; set; } = true;

    public Location Location { get; set; } = Location.None;
}

internal readonly struct GeneratedSource
{
    public GeneratedSource(string hintName, string source)
    {
        HintName = hintName;
        Source = source;
    }

    public string HintName { get; }

    public string Source { get; }
}
