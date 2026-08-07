// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Microsoft.CodeAnalysis;

namespace ProDataGrid.SourceGenerators;

internal static class GeneratorDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidTarget = Create(
        "PDGSG001",
        "Invalid source generation target",
        "Type '{0}' cannot be used for ProDataGrid source generation: {1}");

    public static readonly DiagnosticDescriptor NoColumns = Create(
        "PDGSG002",
        "No eligible columns",
        "Type '{0}' does not contain any eligible properties for generated columns",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor UnsupportedProperty = Create(
        "PDGSG003",
        "Unsupported column property",
        "Property '{0}' cannot be generated as a ProDataGrid column: {1}");

    public static readonly DiagnosticDescriptor InvalidCustomizationMethod = Create(
        "PDGSG004",
        "Invalid customization method",
        "Customization method '{0}' on type '{1}' was not found or has an incompatible signature");

    public static readonly DiagnosticDescriptor ViewModelMustBePartial = Create(
        "PDGSG005",
        "View model must be partial",
        "View model '{0}' and each containing type must be partial to receive generated ProDataGrid members");

    public static readonly DiagnosticDescriptor MemberCollision = Create(
        "PDGSG006",
        "Generated member collision",
        "Type '{0}' already defines member '{1}', so the ProDataGrid member was not generated");

    public static readonly DiagnosticDescriptor InvalidImplementation = Create(
        "PDGSG007",
        "Invalid user implementation",
        "Implementation type '{0}' must be accessible, have an accessible parameterless constructor, and implement IDataGridGeneratedSchema<{1}>");

    public static readonly DiagnosticDescriptor InvalidNamespace = Create(
        "PDGSG008",
        "Invalid namespace target",
        "Namespace '{0}' did not match any eligible source types",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor InvalidColumnConfiguration = Create(
        "PDGSG009",
        "Invalid column configuration",
        "Property '{0}' uses column kind '{1}' but required option '{2}' was not supplied");

    public static readonly DiagnosticDescriptor InaccessibleProperty = Create(
        "PDGSG010",
        "Inaccessible column property",
        "Property '{0}' is not accessible to generated code");

    public static readonly DiagnosticDescriptor AmbiguousItemsProperty = Create(
        "PDGSG011",
        "Cannot infer view-model item type",
        "View model '{0}' does not expose an unambiguous enumerable property named '{1}'",
        DiagnosticSeverity.Warning);

    public static readonly DiagnosticDescriptor MissingViewMember = Create(
        "PDGSG012",
        "Missing generated-view binding member",
        "View model '{0}' does not expose a readable property named '{1}' required by generated view '{2}'");

    public static readonly DiagnosticDescriptor InvalidViewBase = Create(
        "PDGSG013",
        "Invalid generated-view base type",
        "Base type '{0}' for generated view '{1}' must be accessible, non-sealed, and have an accessible parameterless constructor");

    public static readonly DiagnosticDescriptor MissingViewFramework = Create(
        "PDGSG014",
        "Generated-view framework is unavailable",
        "Generated view '{0}' requests framework '{1}', but its required UI framework type is not referenced");

    private static DiagnosticDescriptor Create(
        string id,
        string title,
        string message,
        DiagnosticSeverity severity = DiagnosticSeverity.Error)
    {
        return new DiagnosticDescriptor(
            id,
            title,
            message,
            "ProDataGrid.SourceGeneration",
            severity,
            isEnabledByDefault: true);
    }
}
