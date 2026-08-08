// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ProDataGrid.SourceGenerators;

internal static partial class Discovery
{
    private static ImmutableArray<ViewModelViewModel> DiscoverViews(
        Compilation compilation,
        IReadOnlyList<INamedTypeSymbol> sourceTypes,
        ImmutableArray<AttributeData> assemblyAttributes,
        ImmutableArray<ViewModelModel> generatedViewModels,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var requests = new List<ViewRequest>();

        foreach (AttributeData attribute in assemblyAttributes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsAttribute(attribute, ProDataGridGenerator.GenerateViewsForNamespaceAttributeName))
            {
                DiscoverNamespaceViewRequests(sourceTypes, attribute, requests, diagnostics);
            }
            else if (IsAttribute(attribute, ProDataGridGenerator.GenerateViewAttributeName))
            {
                INamedTypeSymbol? viewModelType = GetConstructorType(attribute, 0);
                INamedTypeSymbol? itemType = GetConstructorType(attribute, 1);
                if (viewModelType == null || itemType == null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        GeneratorDiagnostics.InvalidTarget,
                        GetLocation(attribute),
                        viewModelType?.ToDisplayString() ?? "(unknown)",
                        "assembly-level view generation requires view-model and item types"));
                    continue;
                }

                requests.Add(CreateViewRequest(viewModelType, itemType, attribute));
            }
        }

        bool hasGlobalViewPolicies = HasGlobalViewPolicies(assemblyAttributes);
        if (hasGlobalViewPolicies)
        {
            foreach (INamedTypeSymbol viewModelType in sourceTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (AttributeData attribute in viewModelType.GetAttributes())
                {
                    if (!IsAttribute(attribute, ProDataGridGenerator.GenerateViewAttributeName))
                    {
                        continue;
                    }

                    INamedTypeSymbol? itemType = GetConstructorType(attribute, 0);
                    if (itemType == null)
                    {
                        diagnostics.Add(Diagnostic.Create(
                            GeneratorDiagnostics.InvalidTarget,
                            GetLocation(attribute),
                            viewModelType.ToDisplayString(),
                            "view generation requires an item type"));
                        continue;
                    }

                    requests.Add(CreateViewRequest(viewModelType, itemType, attribute));
                }
            }
        }

        var generatedTypes = new HashSet<INamedTypeSymbol>(
            generatedViewModels.Select(static model => model.ViewModelType),
            SymbolEqualityComparer.Default);
        var views = ImmutableArray.CreateBuilder<ViewModelViewModel>();
        foreach (ViewRequest request in requests
                     .GroupBy(static request => request.ViewNamespace + "." + request.ViewName, StringComparer.Ordinal)
                     .Select(static group => group.Last())
                     .OrderBy(static request => GeneratorUtilities.GetMetadataName(request.ViewModelType), StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ViewModelViewModel? view = ResolveView(compilation, request, generatedTypes, diagnostics);
            if (view != null)
            {
                views.Add(view);
            }
        }

        return views.ToImmutable();
    }

    public static DirectViewCandidate? CreateDirectViewCandidate(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol viewModelType ||
            viewModelType.TypeKind != TypeKind.Class ||
            HasGlobalViewPolicies(viewModelType.ContainingAssembly.GetAttributes()))
        {
            return null;
        }

        return new DirectViewCandidate
        {
            ViewModelType = viewModelType,
            Attributes = context.Attributes,
            CacheKey = CreateDirectSchemaCacheKey(viewModelType, context.Attributes)
        };
    }

    public static DirectViewGenerationResult BuildDirectViews(
        ImmutableArray<DirectViewCandidate> candidates,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var requests = new List<ViewRequest>();
        var generatedViewModels = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        ImmutableArray<AttributeData> assemblyAttributes = compilation.Assembly.GetAttributes();

        foreach (DirectViewCandidate candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsGeneratedViewModel(candidate.ViewModelType, assemblyAttributes))
            {
                generatedViewModels.Add(candidate.ViewModelType);
            }

            foreach (AttributeData attribute in candidate.Attributes)
            {
                INamedTypeSymbol? itemType = GetConstructorType(attribute, 0);
                if (itemType == null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        GeneratorDiagnostics.InvalidTarget,
                        GetLocation(attribute),
                        candidate.ViewModelType.ToDisplayString(),
                        "view generation requires an item type"));
                    continue;
                }

                requests.Add(CreateViewRequest(candidate.ViewModelType, itemType, attribute));
            }
        }

        var sources = ImmutableArray.CreateBuilder<GeneratedSource>();
        foreach (ViewRequest request in requests
                     .GroupBy(static request => request.ViewNamespace + "." + request.ViewName, StringComparer.Ordinal)
                     .Select(static group => group.Last())
                     .OrderBy(static request => GeneratorUtilities.GetMetadataName(request.ViewModelType), StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            ViewModelViewModel? view = ResolveView(compilation, request, generatedViewModels, diagnostics);
            if (view != null)
            {
                sources.Add(Emitter.EmitViewSource(view));
            }
        }

        return new DirectViewGenerationResult(sources.ToImmutable(), diagnostics.ToImmutable());
    }

    private static bool HasGlobalViewPolicies(ImmutableArray<AttributeData> assemblyAttributes)
    {
        foreach (AttributeData attribute in assemblyAttributes)
        {
            if (IsAttribute(attribute, ProDataGridGenerator.GenerateViewAttributeName) ||
                IsAttribute(attribute, ProDataGridGenerator.GenerateViewsForNamespaceAttributeName))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGeneratedViewModel(
        INamedTypeSymbol viewModelType,
        ImmutableArray<AttributeData> assemblyAttributes)
    {
        if (viewModelType.GetAttributes().Any(static attribute =>
                IsAttribute(attribute, ProDataGridGenerator.GenerateViewModelAttributeName)))
        {
            return true;
        }

        foreach (AttributeData attribute in assemblyAttributes)
        {
            if (IsAttribute(attribute, ProDataGridGenerator.GenerateViewModelAttributeName) &&
                SymbolEqualityComparer.Default.Equals(GetConstructorType(attribute, 0), viewModelType))
            {
                return true;
            }

            if (!IsAttribute(attribute, ProDataGridGenerator.GenerateViewModelsForNamespaceAttributeName))
            {
                continue;
            }

            string? namespaceName = GetConstructorString(attribute, 0);
            Dictionary<string, TypedConstant> arguments = GeneratorUtilities.GetNamedArguments(attribute);
            bool includeNested = GeneratorUtilities.GetBoolean(arguments, "IncludeNestedNamespaces", true);
            if (!string.IsNullOrWhiteSpace(namespaceName) &&
                NamespaceMatches(viewModelType, namespaceName!, includeNested))
            {
                return true;
            }
        }

        return false;
    }

    private static void DiscoverNamespaceViewRequests(
        IReadOnlyList<INamedTypeSymbol> sourceTypes,
        AttributeData attribute,
        List<ViewRequest> requests,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        string? namespaceName = GetConstructorString(attribute, 0);
        if (string.IsNullOrWhiteSpace(namespaceName))
        {
            diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.InvalidNamespace, GetLocation(attribute), namespaceName ?? string.Empty));
            return;
        }

        Dictionary<string, TypedConstant> arguments = GeneratorUtilities.GetNamedArguments(attribute);
        bool includeNested = GeneratorUtilities.GetBoolean(arguments, "IncludeNestedNamespaces", true);
        string itemsPropertyName = GeneratorUtilities.GetString(arguments, "ItemsPropertyName") ?? "Items";
        INamedTypeSymbol[] matches = sourceTypes
            .Where(type => type.TypeKind == TypeKind.Class && NamespaceMatches(type, namespaceName!, includeNested))
            .ToArray();
        if (matches.Length == 0)
        {
            diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.InvalidNamespace, GetLocation(attribute), namespaceName));
        }

        foreach (INamedTypeSymbol viewModelType in matches)
        {
            INamedTypeSymbol? itemType = InferItemType(viewModelType, itemsPropertyName);
            if (itemType == null)
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.AmbiguousItemsProperty,
                    GeneratorUtilities.GetLocation(viewModelType),
                    viewModelType.ToDisplayString(),
                    itemsPropertyName));
                continue;
            }

            requests.Add(CreateViewRequest(viewModelType, itemType, attribute));
        }
    }

    private static ViewRequest CreateViewRequest(
        INamedTypeSymbol viewModelType,
        INamedTypeSymbol itemType,
        AttributeData attribute)
    {
        Dictionary<string, TypedConstant> arguments = GeneratorUtilities.GetNamedArguments(attribute);
        string defaultName = viewModelType.Name.EndsWith("ViewModel", StringComparison.Ordinal)
            ? viewModelType.Name.Substring(0, viewModelType.Name.Length - "ViewModel".Length) + "View"
            : viewModelType.Name + "View";
        string viewModelNamespace = viewModelType.ContainingNamespace?.IsGlobalNamespace == false
            ? viewModelType.ContainingNamespace.ToDisplayString()
            : string.Empty;
        string defaultNamespace = viewModelNamespace.EndsWith(".ViewModels", StringComparison.Ordinal)
            ? viewModelNamespace.Substring(0, viewModelNamespace.Length - ".ViewModels".Length) + ".Views"
            : viewModelNamespace;

        return new ViewRequest
        {
            ViewModelType = viewModelType,
            ItemType = itemType,
            ViewName = GeneratorUtilities.SanitizeIdentifier(GeneratorUtilities.GetString(arguments, "ViewName") ?? defaultName),
            ViewNamespace = GeneratorUtilities.GetString(arguments, "ViewNamespace") ?? defaultNamespace,
            Framework = GetViewFramework(arguments),
            BaseType = GeneratorUtilities.GetType(arguments, "BaseType"),
            Title = GeneratorUtilities.GetString(arguments, "Title") ?? SplitWords(defaultName.Replace("View", string.Empty)),
            ItemsPropertyName = GeneratorUtilities.GetString(arguments, "ItemsPropertyName") ?? "Items",
            ColumnDefinitionsPropertyName = GeneratorUtilities.GetString(arguments, "ColumnDefinitionsPropertyName") ?? "ColumnDefinitions",
            FastPathOptionsPropertyName = GeneratorUtilities.GetString(arguments, "FastPathOptionsPropertyName") ?? "FastPathOptions",
            SortingModelPropertyName = GeneratorUtilities.GetString(arguments, "SortingModelPropertyName"),
            FilteringModelPropertyName = GeneratorUtilities.GetString(arguments, "FilteringModelPropertyName"),
            SearchModelPropertyName = GeneratorUtilities.GetString(arguments, "SearchModelPropertyName"),
            SearchTextPropertyName = GeneratorUtilities.GetString(arguments, "SearchTextPropertyName"),
            SelectionModelPropertyName = GeneratorUtilities.GetString(arguments, "SelectionModelPropertyName"),
            StateControllerPropertyName = GeneratorUtilities.GetString(arguments, "StateControllerPropertyName"),
            Recipe = GetEnumValue(arguments, "Recipe", 1),
            ControllerName = GeneratorUtilities.GetString(arguments, "ControllerName"),
            AutomationId = GeneratorUtilities.GetString(arguments, "AutomationId") ?? defaultName,
            Location = GetLocation(attribute)
        };
    }

    private static ViewModelViewModel? ResolveView(
        Compilation compilation,
        ViewRequest request,
        HashSet<INamedTypeSymbol> generatedViewModels,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (request.ViewModelType.TypeParameters.Length != 0)
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.InvalidTarget,
                request.Location,
                request.ViewModelType.ToDisplayString(),
                "open generic generated views are not supported"));
            return null;
        }

        string metadataName = string.IsNullOrEmpty(request.ViewNamespace)
            ? request.ViewName
            : request.ViewNamespace + "." + request.ViewName;
        if (compilation.GetTypeByMetadataName(metadataName) != null)
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.InvalidTarget,
                request.Location,
                metadataName,
                "a type with the generated view name already exists"));
            return null;
        }

        string requiredFrameworkType = request.Framework == ViewFrameworkModel.ReactiveUI
            ? "ReactiveUI.Avalonia.ReactiveUserControl`1"
            : "Avalonia.Controls.UserControl";
        if (request.BaseType == null && compilation.GetTypeByMetadataName(requiredFrameworkType) == null)
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.MissingViewFramework,
                request.Location,
                metadataName,
                request.Framework.ToString()));
            return null;
        }

        if (request.BaseType != null && !ValidateViewBase(request.BaseType))
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.InvalidViewBase,
                request.Location,
                request.BaseType.ToDisplayString(),
                metadataName));
            return null;
        }

        bool generatedViewModel = generatedViewModels.Contains(request.ViewModelType);
        ViewBindingModel? items = ResolveViewBinding(request, request.ItemsPropertyName, generatedFallbackType: null, canUseGeneratedFallback: false, diagnostics);
        ViewBindingModel? columns = ResolveViewBinding(
            request,
            request.ColumnDefinitionsPropertyName,
            "global::Avalonia.Controls.DataGridColumnDefinitionList",
            generatedViewModel,
            diagnostics);
        ViewBindingModel? fastOptions = ResolveViewBinding(
            request,
            request.FastPathOptionsPropertyName,
            "global::Avalonia.Controls.DataGridFastPathOptions",
            generatedViewModel,
            diagnostics);
        if (items == null || columns == null || fastOptions == null)
        {
            return null;
        }

        return new ViewModelViewModel
        {
            ViewModelType = request.ViewModelType,
            ItemType = request.ItemType,
            ViewName = request.ViewName,
            ViewNamespace = request.ViewNamespace,
            Framework = request.Framework,
            BaseType = request.BaseType,
            Title = request.Title,
            Recipe = request.Recipe,
            ControllerName = request.ControllerName,
            AutomationId = request.AutomationId,
            Items = items,
            ColumnDefinitions = columns,
            FastPathOptions = fastOptions,
            SortingModel = ResolveOptionalViewBinding(request, request.SortingModelPropertyName, diagnostics),
            FilteringModel = ResolveOptionalViewBinding(request, request.FilteringModelPropertyName, diagnostics),
            SearchModel = ResolveOptionalViewBinding(request, request.SearchModelPropertyName, diagnostics),
            SearchText = ResolveOptionalViewBinding(request, request.SearchTextPropertyName, diagnostics, requireSetter: true),
            SelectionModel = ResolveOptionalViewBinding(request, request.SelectionModelPropertyName, diagnostics),
            StateController = ResolveOptionalViewBinding(request, request.StateControllerPropertyName, diagnostics),
            Location = request.Location
        };
    }

    private static ViewBindingModel? ResolveOptionalViewBinding(
        ViewRequest request,
        string? propertyName,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        bool requireSetter = false)
    {
        return string.IsNullOrWhiteSpace(propertyName)
            ? null
            : ResolveViewBinding(request, propertyName!, null, false, diagnostics, requireSetter);
    }

    private static ViewBindingModel? ResolveViewBinding(
        ViewRequest request,
        string propertyName,
        string? generatedFallbackType,
        bool canUseGeneratedFallback,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        bool requireSetter = false)
    {
        IPropertySymbol? property = request.ViewModelType.GetMembers(propertyName)
            .OfType<IPropertySymbol>()
            .FirstOrDefault(static property => !property.IsStatic && property.GetMethod != null);
        if (property != null && GeneratorUtilities.IsAccessibleFromGeneratedCode(property.GetMethod!))
        {
            bool canWrite = property.SetMethod != null && GeneratorUtilities.IsAccessibleFromGeneratedCode(property.SetMethod);
            if (!requireSetter || canWrite)
            {
                ITypeSymbol runtimeType = UnwrapNullable(property.Type);
                return new ViewBindingModel
                {
                    PropertyName = propertyName,
                    PropertyType = property.Type.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat),
                    RuntimePropertyType = runtimeType.ToDisplayString(GeneratorUtilities.FullyQualifiedFormat),
                    CanWrite = canWrite
                };
            }
        }

        IFieldSymbol? reactiveField = request.ViewModelType.GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(field =>
                !field.IsStatic &&
                string.Equals(GetReactivePropertyName(field.Name), propertyName, StringComparison.Ordinal) &&
                field.GetAttributes().Any(static attribute =>
                    string.Equals(
                        attribute.AttributeClass?.ToDisplayString(),
                        "ReactiveUI.SourceGenerators.ReactiveAttribute",
                        StringComparison.Ordinal)));
        if (reactiveField != null)
        {
            ITypeSymbol runtimeType = UnwrapNullable(reactiveField.Type);
            return new ViewBindingModel
            {
                PropertyName = propertyName,
                PropertyType = reactiveField.Type.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat),
                RuntimePropertyType = runtimeType.ToDisplayString(GeneratorUtilities.FullyQualifiedFormat),
                CanWrite = true
            };
        }

        if (canUseGeneratedFallback && generatedFallbackType != null)
        {
            return new ViewBindingModel
            {
                PropertyName = propertyName,
                PropertyType = generatedFallbackType,
                RuntimePropertyType = generatedFallbackType,
                CanWrite = false
            };
        }

        diagnostics.Add(Diagnostic.Create(
            GeneratorDiagnostics.MissingViewMember,
            request.Location,
            request.ViewModelType.ToDisplayString(),
            propertyName,
            request.ViewName));
        return null;
    }

    private static string GetReactivePropertyName(string fieldName)
    {
        string trimmed = fieldName.TrimStart('_');
        if (trimmed.Length == 0)
        {
            return fieldName;
        }

        return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
    }

    private static bool ValidateViewBase(INamedTypeSymbol baseType)
    {
        if (baseType.IsSealed || !GeneratorUtilities.IsAccessibleFromGeneratedCode(baseType))
        {
            return false;
        }

        bool hasConstructor = baseType.InstanceConstructors.Any(static constructor =>
            constructor.Parameters.Length == 0 && GeneratorUtilities.IsAccessibleFromGeneratedCode(constructor));
        if (!hasConstructor)
        {
            return false;
        }

        INamedTypeSymbol? current = baseType;
        while (current != null)
        {
            if (string.Equals(
                    GeneratorUtilities.GetMetadataName(current.OriginalDefinition),
                    "Avalonia.Controls.UserControl",
                    StringComparison.Ordinal))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }

    private static ViewFrameworkModel GetViewFramework(Dictionary<string, TypedConstant> arguments)
    {
        if (arguments.TryGetValue("Framework", out TypedConstant value) && value.Value is int frameworkValue && frameworkValue == 1)
        {
            return ViewFrameworkModel.ReactiveUI;
        }

        return ViewFrameworkModel.Avalonia;
    }

    private static string SplitWords(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 4);
        for (int index = 0; index < value.Length; index++)
        {
            char character = value[index];
            if (index > 0 && char.IsUpper(character) && !char.IsUpper(value[index - 1]))
            {
                result.Append(' ');
            }

            result.Append(character);
        }

        return result.ToString();
    }

    private sealed class ViewRequest
    {
        public INamedTypeSymbol ViewModelType { get; set; } = null!;
        public INamedTypeSymbol ItemType { get; set; } = null!;
        public string ViewName { get; set; } = string.Empty;
        public string ViewNamespace { get; set; } = string.Empty;
        public ViewFrameworkModel Framework { get; set; }
        public INamedTypeSymbol? BaseType { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Recipe { get; set; }
        public string? ControllerName { get; set; }
        public string AutomationId { get; set; } = string.Empty;
        public string ItemsPropertyName { get; set; } = "Items";
        public string ColumnDefinitionsPropertyName { get; set; } = "ColumnDefinitions";
        public string FastPathOptionsPropertyName { get; set; } = "FastPathOptions";
        public string? SortingModelPropertyName { get; set; }
        public string? FilteringModelPropertyName { get; set; }
        public string? SearchModelPropertyName { get; set; }
        public string? SearchTextPropertyName { get; set; }
        public string? SelectionModelPropertyName { get; set; }
        public string? StateControllerPropertyName { get; set; }
        public Location Location { get; set; } = Location.None;
    }
}
