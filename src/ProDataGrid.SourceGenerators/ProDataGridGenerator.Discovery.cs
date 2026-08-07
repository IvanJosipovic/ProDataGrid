// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProDataGrid.SourceGenerators;

internal static partial class Discovery
{
    public static GenerationModel Build(Compilation compilation, CancellationToken cancellationToken)
    {
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        INamedTypeSymbol[] sourceTypes = GeneratorUtilities
            .EnumerateTypes(compilation.Assembly.GlobalNamespace)
            .Where(static type => type.Locations.Any(static location => location.IsInSource))
            .OrderBy(GeneratorUtilities.GetMetadataName, StringComparer.Ordinal)
            .ToArray();

        var schemas = new Dictionary<INamedTypeSymbol, SchemaModel>(SymbolEqualityComparer.Default);
        var viewModels = new Dictionary<INamedTypeSymbol, PendingViewModel>(SymbolEqualityComparer.Default);
        ImmutableArray<AttributeData> assemblyAttributes = compilation.Assembly.GetAttributes();

        DiscoverNamespaceSchemas(sourceTypes, assemblyAttributes, schemas, diagnostics, cancellationToken);
        DiscoverAssemblySchemas(assemblyAttributes, schemas, diagnostics);
        DiscoverTypeAndPropertySchemas(sourceTypes, schemas, diagnostics, cancellationToken);

        DiscoverNamespaceViewModels(sourceTypes, assemblyAttributes, schemas, viewModels, diagnostics, cancellationToken);
        DiscoverAssemblyViewModels(assemblyAttributes, schemas, viewModels, diagnostics);
        DiscoverTypeViewModels(sourceTypes, schemas, viewModels, diagnostics, cancellationToken);

        ResolveProviderCollisions(schemas.Values);

        foreach (SchemaModel schema in schemas.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ValidateSchemaTarget(schema, diagnostics))
            {
                schema.Columns = ImmutableArray<ColumnModel>.Empty;
                continue;
            }

            if (schema.ImplementationType != null &&
                !ValidateImplementation(schema.ItemType, schema.ImplementationType))
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.InvalidImplementation,
                    schema.Location,
                    schema.ImplementationType.ToDisplayString(),
                    schema.ItemType.ToDisplayString()));
                schema.ImplementationType = null;
            }

            schema.Columns = DiscoverColumns(schema, diagnostics, cancellationToken);
            if (schema.Columns.Length == 0 && schema.ImplementationType == null)
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.NoColumns,
                    schema.Location,
                    schema.ItemType.ToDisplayString()));
            }

            if (!string.IsNullOrEmpty(schema.ConfigureMethod) &&
                !HasGlobalConfigureMethod(compilation, schema.ItemType, schema.ConfigureMethod!))
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.InvalidCustomizationMethod,
                    schema.Location,
                    schema.ConfigureMethod,
                    schema.ItemType.ToDisplayString()));
                schema.ConfigureMethod = null;
            }
        }

        var resolvedViewModels = ImmutableArray.CreateBuilder<ViewModelModel>();
        foreach (PendingViewModel pending in viewModels.Values.OrderBy(
                     static model => GeneratorUtilities.GetMetadataName(model.ViewModelType),
                     StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!AllContainingTypesArePartial(pending.ViewModelType))
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.ViewModelMustBePartial,
                    pending.Location,
                    pending.ViewModelType.ToDisplayString()));
                continue;
            }

            if (pending.ViewModelType.TypeParameters.Length != 0)
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.InvalidTarget,
                    pending.Location,
                    pending.ViewModelType.ToDisplayString(),
                    "open generic view models are not supported"));
                continue;
            }

            SchemaModel schema = schemas[pending.ItemType];
            var model = new ViewModelModel
            {
                ViewModelType = pending.ViewModelType,
                Schema = schema,
                ColumnDefinitionsPropertyName = pending.ColumnDefinitionsPropertyName,
                SchemaPropertyName = pending.SchemaPropertyName,
                FastPathOptionsPropertyName = pending.FastPathOptionsPropertyName,
                Location = pending.Location
            };

            model.GenerateColumnDefinitionsProperty = ValidateGeneratedMember(
                pending.ViewModelType,
                model.ColumnDefinitionsPropertyName,
                diagnostics,
                pending.Location);
            model.GenerateSchemaProperty = ValidateGeneratedMember(
                pending.ViewModelType,
                model.SchemaPropertyName,
                diagnostics,
                pending.Location);
            model.GenerateFastPathOptionsProperty = ValidateGeneratedMember(
                pending.ViewModelType,
                model.FastPathOptionsPropertyName,
                diagnostics,
                pending.Location);
            resolvedViewModels.Add(model);
        }

        ImmutableArray<SchemaModel> orderedSchemas = schemas.Values
            .OrderBy(static schema => schema.ProviderNamespace, StringComparer.Ordinal)
            .ThenBy(static schema => schema.ProviderName, StringComparer.Ordinal)
            .ToImmutableArray();

        ImmutableArray<ViewModelViewModel> views = DiscoverViews(
            compilation,
            sourceTypes,
            assemblyAttributes,
            resolvedViewModels.ToImmutable(),
            diagnostics,
            cancellationToken);

        return new GenerationModel(orderedSchemas, resolvedViewModels.ToImmutable(), views, diagnostics.ToImmutable());
    }

    private static void DiscoverNamespaceSchemas(
        IReadOnlyList<INamedTypeSymbol> sourceTypes,
        ImmutableArray<AttributeData> assemblyAttributes,
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (AttributeData attribute in assemblyAttributes)
        {
            if (!IsAttribute(attribute, ProDataGridGenerator.GenerateColumnsForNamespaceAttributeName))
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            string? namespaceName = GetConstructorString(attribute, 0);
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.InvalidNamespace, GetLocation(attribute), namespaceName ?? string.Empty));
                continue;
            }

            Dictionary<string, TypedConstant> arguments = GeneratorUtilities.GetNamedArguments(attribute);
            bool includeNested = GeneratorUtilities.GetBoolean(arguments, "IncludeNestedNamespaces", true);
            INamedTypeSymbol[] matches = sourceTypes
                .Where(type => NamespaceMatches(type, namespaceName!, includeNested) && IsEligibleItemType(type))
                .ToArray();
            if (matches.Length == 0)
            {
                diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.InvalidNamespace, GetLocation(attribute), namespaceName));
            }

            foreach (INamedTypeSymbol type in matches)
            {
                AddOrUpdateSchema(schemas, type, attribute, explicitProviderName: null, explicitConfiguration: false);
            }
        }
    }

    private static void DiscoverAssemblySchemas(
        ImmutableArray<AttributeData> assemblyAttributes,
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        foreach (AttributeData attribute in assemblyAttributes)
        {
            if (!IsAttribute(attribute, ProDataGridGenerator.GenerateColumnsAttributeName))
            {
                continue;
            }

            INamedTypeSymbol? itemType = GetConstructorType(attribute, 0);
            if (itemType == null)
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.InvalidTarget,
                    GetLocation(attribute),
                    "(unknown)",
                    "assembly-level generation requires an item type"));
                continue;
            }

            AddOrUpdateSchema(schemas, itemType, attribute, explicitProviderName: null, explicitConfiguration: true);
        }
    }

    private static void DiscoverTypeAndPropertySchemas(
        IReadOnlyList<INamedTypeSymbol> sourceTypes,
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (INamedTypeSymbol type in sourceTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (AttributeData attribute in type.GetAttributes())
            {
                if (!IsAttribute(attribute, ProDataGridGenerator.GenerateColumnsAttributeName))
                {
                    continue;
                }

                INamedTypeSymbol itemType = GetConstructorType(attribute, 0) ?? type;
                AddOrUpdateSchema(schemas, itemType, attribute, explicitProviderName: null, explicitConfiguration: true);
            }

            bool hasColumnAttribute = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(static property => GeneratorUtilities.HasAttribute(property, ProDataGridGenerator.ColumnAttributeName));
            if (hasColumnAttribute && !schemas.ContainsKey(type))
            {
                schemas.Add(type, CreateDefaultSchema(type, GeneratorUtilities.GetLocation(type), attributedOnly: true));
            }
        }
    }

    private static void DiscoverNamespaceViewModels(
        IReadOnlyList<INamedTypeSymbol> sourceTypes,
        ImmutableArray<AttributeData> assemblyAttributes,
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        Dictionary<INamedTypeSymbol, PendingViewModel> viewModels,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (AttributeData attribute in assemblyAttributes)
        {
            if (!IsAttribute(attribute, ProDataGridGenerator.GenerateViewModelsForNamespaceAttributeName))
            {
                continue;
            }

            cancellationToken.ThrowIfCancellationRequested();
            string? namespaceName = GetConstructorString(attribute, 0);
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                diagnostics.Add(Diagnostic.Create(GeneratorDiagnostics.InvalidNamespace, GetLocation(attribute), namespaceName ?? string.Empty));
                continue;
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

                SchemaModel schema = EnsureSchema(schemas, itemType, attribute, explicitProviderName: null);
                ApplyFastOptions(schema, arguments);
                viewModels[viewModelType] = CreatePendingViewModel(viewModelType, itemType, attribute, arguments);
            }
        }
    }

    private static void DiscoverAssemblyViewModels(
        ImmutableArray<AttributeData> assemblyAttributes,
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        Dictionary<INamedTypeSymbol, PendingViewModel> viewModels,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        foreach (AttributeData attribute in assemblyAttributes)
        {
            if (!IsAttribute(attribute, ProDataGridGenerator.GenerateViewModelAttributeName))
            {
                continue;
            }

            INamedTypeSymbol? viewModelType = GetConstructorType(attribute, 0);
            INamedTypeSymbol? itemType = GetConstructorType(attribute, 1);
            if (viewModelType == null || itemType == null)
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.InvalidTarget,
                    GetLocation(attribute),
                    viewModelType?.ToDisplayString() ?? "(unknown)",
                    "assembly-level view-model generation requires view-model and item types"));
                continue;
            }

            Dictionary<string, TypedConstant> arguments = GeneratorUtilities.GetNamedArguments(attribute);
            string? providerName = GeneratorUtilities.GetString(arguments, "ProviderName");
            SchemaModel schema = EnsureSchema(schemas, itemType, attribute, providerName);
            ApplyFastOptions(schema, arguments);
            viewModels[viewModelType] = CreatePendingViewModel(viewModelType, itemType, attribute, arguments);
        }
    }

    private static void DiscoverTypeViewModels(
        IReadOnlyList<INamedTypeSymbol> sourceTypes,
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        Dictionary<INamedTypeSymbol, PendingViewModel> viewModels,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        foreach (INamedTypeSymbol viewModelType in sourceTypes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (AttributeData attribute in viewModelType.GetAttributes())
            {
                if (!IsAttribute(attribute, ProDataGridGenerator.GenerateViewModelAttributeName))
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
                        "view-model generation requires an item type"));
                    continue;
                }

                Dictionary<string, TypedConstant> arguments = GeneratorUtilities.GetNamedArguments(attribute);
                string? providerName = GeneratorUtilities.GetString(arguments, "ProviderName");
                SchemaModel schema = EnsureSchema(schemas, itemType, attribute, providerName);
                ApplyFastOptions(schema, arguments);
                viewModels[viewModelType] = CreatePendingViewModel(viewModelType, itemType, attribute, arguments);
            }
        }
    }

    private static SchemaModel EnsureSchema(
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        INamedTypeSymbol itemType,
        AttributeData attribute,
        string? explicitProviderName)
    {
        if (!schemas.TryGetValue(itemType, out SchemaModel? schema))
        {
            schema = CreateDefaultSchema(itemType, GetLocation(attribute), attributedOnly: false);
            schemas.Add(itemType, schema);
        }

        if (!string.IsNullOrWhiteSpace(explicitProviderName))
        {
            schema.ProviderName = GeneratorUtilities.SanitizeIdentifier(explicitProviderName!);
        }

        return schema;
    }

    private static void AddOrUpdateSchema(
        Dictionary<INamedTypeSymbol, SchemaModel> schemas,
        INamedTypeSymbol itemType,
        AttributeData attribute,
        string? explicitProviderName,
        bool explicitConfiguration)
    {
        SchemaModel schema = EnsureSchema(schemas, itemType, attribute, explicitProviderName);
        Dictionary<string, TypedConstant> arguments = GeneratorUtilities.GetNamedArguments(attribute);
        string? providerName = explicitProviderName ?? GeneratorUtilities.GetString(arguments, "ProviderName");
        string? providerNamespace = GeneratorUtilities.GetString(arguments, "ProviderNamespace");
        if (!string.IsNullOrWhiteSpace(providerName))
        {
            schema.ProviderName = GeneratorUtilities.SanitizeIdentifier(providerName!);
        }

        if (providerNamespace != null)
        {
            schema.ProviderNamespace = providerNamespace;
        }

        if (explicitConfiguration)
        {
            schema.AttributedOnly = GetEnumValue(arguments, "Discovery", 0) == 1;
            schema.IncludeInherited = GeneratorUtilities.GetBoolean(arguments, "IncludeInherited", true);
            schema.ConfigureMethod = GeneratorUtilities.GetString(arguments, "ConfigureMethod");
            if (arguments.TryGetValue("ImplementationType", out TypedConstant implementation) &&
                implementation.Value is INamedTypeSymbol implementationType)
            {
                schema.ImplementationType = implementationType;
            }
        }

        ApplyFastOptions(schema, arguments);
    }

    private static void ApplyFastOptions(SchemaModel schema, Dictionary<string, TypedConstant> arguments)
    {
        schema.Strict = GeneratorUtilities.GetBoolean(arguments, "Strict", schema.Strict);
        schema.Streaming = GeneratorUtilities.GetBoolean(arguments, "Streaming", schema.Streaming);
    }

    private static SchemaModel CreateDefaultSchema(INamedTypeSymbol itemType, Location location, bool attributedOnly)
    {
        return new SchemaModel
        {
            ItemType = itemType,
            ProviderName = GeneratorUtilities.GetDefaultProviderName(itemType),
            ProviderNamespace = itemType.ContainingNamespace?.IsGlobalNamespace == false
                ? itemType.ContainingNamespace.ToDisplayString()
                : string.Empty,
            AttributedOnly = attributedOnly,
            Location = location
        };
    }

    private static ImmutableArray<ColumnModel> DiscoverColumns(
        SchemaModel schema,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        var properties = new Dictionary<string, IPropertySymbol>(StringComparer.Ordinal);
        INamedTypeSymbol? current = schema.ItemType;
        while (current != null)
        {
            foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!properties.ContainsKey(property.Name))
                {
                    properties.Add(property.Name, property);
                }
            }

            current = schema.IncludeInherited ? current.BaseType : null;
        }

        var columns = ImmutableArray.CreateBuilder<ColumnModel>();
        foreach (IPropertySymbol property in properties.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AttributeData? columnAttribute = GeneratorUtilities.FindAttribute(property, ProDataGridGenerator.ColumnAttributeName);
            if (GeneratorUtilities.HasAttribute(property, ProDataGridGenerator.IgnoreColumnAttributeName) ||
                (schema.AttributedOnly && columnAttribute == null))
            {
                continue;
            }

            string? unsupportedReason = GetUnsupportedPropertyReason(property);
            if (unsupportedReason != null)
            {
                if (columnAttribute != null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        GeneratorDiagnostics.UnsupportedProperty,
                        GeneratorUtilities.GetLocation(property),
                        property.ToDisplayString(),
                        unsupportedReason));
                }

                continue;
            }

            Dictionary<string, TypedConstant> options = GeneratorUtilities.GetNamedArguments(columnAttribute);
            string kind = GetColumnKind(property.Type, columnAttribute, options);
            string? header = GeneratorUtilities.GetString(options, "Header");
            string columnKey = GeneratorUtilities.GetString(options, "ColumnKey") ?? property.Name;
            string? configureMethod = GeneratorUtilities.GetString(options, "ConfigureMethod");
            string? factoryMethod = GeneratorUtilities.GetString(options, "FactoryMethod");
            bool searchable = GeneratorUtilities.GetBoolean(options, "IsSearchable", true);

            if (!string.IsNullOrEmpty(configureMethod) &&
                !HasColumnConfigureMethod(schema.ItemType, configureMethod!, kind))
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.InvalidCustomizationMethod,
                    GeneratorUtilities.GetLocation(property),
                    configureMethod,
                    schema.ItemType.ToDisplayString()));
                configureMethod = null;
            }

            if (!string.IsNullOrEmpty(factoryMethod) &&
                !HasColumnFactoryMethod(schema.ItemType, factoryMethod!))
            {
                diagnostics.Add(Diagnostic.Create(
                    GeneratorDiagnostics.InvalidCustomizationMethod,
                    GeneratorUtilities.GetLocation(property),
                    factoryMethod,
                    schema.ItemType.ToDisplayString()));
                factoryMethod = null;
            }

            ValidateRequiredKindOptions(property, kind, options, diagnostics);

            int sourceOrder = property.Locations.FirstOrDefault(static location => location.IsInSource)?.SourceSpan.Start ?? int.MaxValue;
            columns.Add(new ColumnModel
            {
                Property = property,
                Kind = kind,
                Header = header ?? GeneratorUtilities.ToHeader(property.Name),
                Order = GeneratorUtilities.GetInt32(options, "Order", 0),
                SourceOrder = sourceOrder,
                Options = options.ToImmutableDictionary(StringComparer.Ordinal),
                ColumnKey = columnKey,
                ConfigureMethod = configureMethod,
                FactoryMethod = factoryMethod,
                IsSearchable = searchable
            });
        }

        return columns
            .OrderBy(static column => column.Order)
            .ThenBy(static column => column.SourceOrder)
            .ThenBy(static column => column.Property.Name, StringComparer.Ordinal)
            .ToImmutableArray();
    }

    private static bool ValidateSchemaTarget(SchemaModel schema, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        INamedTypeSymbol type = schema.ItemType;
        string? reason = null;
        if (type.TypeKind != TypeKind.Class && type.TypeKind != TypeKind.Struct)
        {
            reason = "only classes and structs are supported";
        }
        else if (type.IsUnboundGenericType || type.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter))
        {
            reason = "open generic item types are not supported";
        }
        else if (!GeneratorUtilities.IsAccessibleFromGeneratedCode(type))
        {
            reason = "the item type is inaccessible to generated code";
        }

        if (reason == null)
        {
            return true;
        }

        diagnostics.Add(Diagnostic.Create(
            GeneratorDiagnostics.InvalidTarget,
            schema.Location,
            type.ToDisplayString(),
            reason));
        return false;
    }

    private static string? GetUnsupportedPropertyReason(IPropertySymbol property)
    {
        if (property.IsStatic)
        {
            return "static properties are not supported";
        }

        if (property.IsIndexer)
        {
            return "indexers are not supported";
        }

        if (property.GetMethod == null || !GeneratorUtilities.IsAccessibleFromGeneratedCode(property.GetMethod))
        {
            return "the getter is not accessible";
        }

        if (property.ReturnsByRef || property.ReturnsByRefReadonly)
        {
            return "by-reference properties are not supported";
        }

        if (property.Type.TypeKind == TypeKind.Pointer || property.Type.TypeKind == TypeKind.FunctionPointer)
        {
            return "pointer properties are not supported";
        }

        return null;
    }

    private static string GetColumnKind(
        ITypeSymbol propertyType,
        AttributeData? attribute,
        Dictionary<string, TypedConstant> options)
    {
        int kindValue = GetEnumValue(options, "Kind", -1);
        if (kindValue < 0 && attribute != null && attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is int constructorKind)
        {
            kindValue = constructorKind;
        }

        string[] kinds =
        {
            "Auto", "Text", "CheckBox", "Hyperlink", "Image", "Numeric", "ProgressBar", "Slider",
            "DatePicker", "TimePicker", "MaskedText", "AutoComplete", "ToggleButton", "ToggleSwitch",
            "Hierarchical", "CustomDrawing", "ComboBoxSelectedItem", "ComboBoxSelectedValue", "ComboBoxText",
            "Template", "Button", "Formula"
        };
        if (kindValue > 0 && kindValue < kinds.Length)
        {
            return kinds[kindValue];
        }

        ITypeSymbol effectiveType = UnwrapNullable(propertyType);
        if (effectiveType.TypeKind == TypeKind.Enum)
        {
            return "ComboBoxSelectedItem";
        }

        switch (effectiveType.SpecialType)
        {
            case SpecialType.System_Boolean:
                return "CheckBox";
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
                return "Numeric";
        }

        string metadataName = effectiveType is INamedTypeSymbol named
            ? GeneratorUtilities.GetMetadataName(named)
            : effectiveType.ToDisplayString();
        if (metadataName == "System.DateTime" || metadataName == "System.DateTimeOffset")
        {
            return "DatePicker";
        }

        if (metadataName == "System.TimeSpan")
        {
            return "TimePicker";
        }

        if (metadataName == "System.Uri")
        {
            return "Hyperlink";
        }

        return "Text";
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named &&
            named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }

        return type;
    }

    private static void ValidateRequiredKindOptions(
        IPropertySymbol property,
        string kind,
        Dictionary<string, TypedConstant> options,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        string? required = null;
        if (kind == "Template" && string.IsNullOrEmpty(GeneratorUtilities.GetString(options, "TemplateKey")))
        {
            required = "TemplateKey";
        }
        else if (kind == "Formula" && string.IsNullOrEmpty(GeneratorUtilities.GetString(options, "Formula")))
        {
            required = "Formula";
        }

        if (required != null)
        {
            diagnostics.Add(Diagnostic.Create(
                GeneratorDiagnostics.InvalidColumnConfiguration,
                GeneratorUtilities.GetLocation(property),
                property.ToDisplayString(),
                kind,
                required));
        }
    }

    private static bool HasGlobalConfigureMethod(Compilation compilation, INamedTypeSymbol type, string name)
    {
        INamedTypeSymbol? listType = compilation.GetTypeByMetadataName("Avalonia.Controls.DataGridColumnDefinitionList");
        return type.GetMembers(name).OfType<IMethodSymbol>().Any(method =>
            method.IsStatic &&
            GeneratorUtilities.IsAccessibleFromGeneratedCode(method) &&
            method.ReturnsVoid &&
            method.Parameters.Length == 1 &&
            (listType == null || SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, listType)));
    }

    private static bool HasColumnConfigureMethod(INamedTypeSymbol type, string name, string kind)
    {
        foreach (IMethodSymbol method in type.GetMembers(name).OfType<IMethodSymbol>())
        {
            if (!method.IsStatic || !GeneratorUtilities.IsAccessibleFromGeneratedCode(method) || !method.ReturnsVoid || method.Parameters.Length != 1)
            {
                continue;
            }

            string parameterName = method.Parameters[0].Type.Name;
            if (parameterName == "DataGridColumnDefinition" || parameterName == "DataGrid" + kind + "ColumnDefinition")
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasColumnFactoryMethod(INamedTypeSymbol type, string name)
    {
        return type.GetMembers(name).OfType<IMethodSymbol>().Any(method =>
            method.IsStatic &&
            GeneratorUtilities.IsAccessibleFromGeneratedCode(method) &&
            method.Parameters.Length == 0 &&
            IsOrDerivesFrom(method.ReturnType, "Avalonia.Controls.DataGridColumnDefinition"));
    }

    private static bool IsOrDerivesFrom(ITypeSymbol? type, string metadataName)
    {
        ITypeSymbol? current = type;
        while (current is INamedTypeSymbol named)
        {
            if (string.Equals(GeneratorUtilities.GetMetadataName(named), metadataName, StringComparison.Ordinal))
            {
                return true;
            }

            current = named.BaseType;
        }

        return false;
    }

    private static bool ValidateImplementation(INamedTypeSymbol itemType, INamedTypeSymbol implementationType)
    {
        if (!GeneratorUtilities.IsAccessibleFromGeneratedCode(implementationType) || implementationType.IsAbstract)
        {
            return false;
        }

        bool hasConstructor = implementationType.InstanceConstructors.Any(static constructor =>
            constructor.Parameters.Length == 0 && GeneratorUtilities.IsAccessibleFromGeneratedCode(constructor));
        if (!hasConstructor)
        {
            return false;
        }

        foreach (INamedTypeSymbol implemented in implementationType.AllInterfaces)
        {
            if (string.Equals(
                    GeneratorUtilities.GetMetadataName(implemented.OriginalDefinition),
                    "Avalonia.Controls.IDataGridGeneratedSchema`1",
                    StringComparison.Ordinal) &&
                implemented.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(implemented.TypeArguments[0], itemType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ValidateGeneratedMember(
        INamedTypeSymbol viewModelType,
        string memberName,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        Location location)
    {
        if (viewModelType.GetMembers(memberName).Length == 0)
        {
            return true;
        }

        diagnostics.Add(Diagnostic.Create(
            GeneratorDiagnostics.MemberCollision,
            location,
            viewModelType.ToDisplayString(),
            memberName));
        return false;
    }

    private static bool AllContainingTypesArePartial(INamedTypeSymbol type)
    {
        INamedTypeSymbol? current = type;
        while (current != null)
        {
            if (!GeneratorUtilities.IsPartial(current))
            {
                return false;
            }

            current = current.ContainingType;
        }

        return true;
    }

    private static INamedTypeSymbol? InferItemType(INamedTypeSymbol viewModelType, string itemsPropertyName)
    {
        IPropertySymbol[] candidates = viewModelType.GetMembers(itemsPropertyName)
            .OfType<IPropertySymbol>()
            .Where(static property => !property.IsStatic && property.GetMethod != null)
            .ToArray();
        if (candidates.Length != 1)
        {
            return null;
        }

        ITypeSymbol type = candidates[0].Type;
        if (type is IArrayTypeSymbol array)
        {
            return array.ElementType as INamedTypeSymbol;
        }

        if (type is INamedTypeSymbol named)
        {
            if (named.IsGenericType && named.TypeArguments.Length == 1 && IsEnumerableDefinition(named.OriginalDefinition))
            {
                return named.TypeArguments[0] as INamedTypeSymbol;
            }

            foreach (INamedTypeSymbol implemented in named.AllInterfaces)
            {
                if (implemented.IsGenericType && implemented.TypeArguments.Length == 1 && IsEnumerableDefinition(implemented.OriginalDefinition))
                {
                    return implemented.TypeArguments[0] as INamedTypeSymbol;
                }
            }
        }

        return null;
    }

    private static bool IsEnumerableDefinition(INamedTypeSymbol type)
    {
        return type.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
    }

    private static PendingViewModel CreatePendingViewModel(
        INamedTypeSymbol viewModelType,
        INamedTypeSymbol itemType,
        AttributeData attribute,
        Dictionary<string, TypedConstant> arguments)
    {
        return new PendingViewModel
        {
            ViewModelType = viewModelType,
            ItemType = itemType,
            ColumnDefinitionsPropertyName = GeneratorUtilities.GetString(arguments, "ColumnDefinitionsPropertyName") ?? "ColumnDefinitions",
            SchemaPropertyName = GeneratorUtilities.GetString(arguments, "SchemaPropertyName") ?? "DataGridSchema",
            FastPathOptionsPropertyName = GeneratorUtilities.GetString(arguments, "FastPathOptionsPropertyName") ?? "FastPathOptions",
            Location = GetLocation(attribute)
        };
    }

    private static void ResolveProviderCollisions(IEnumerable<SchemaModel> schemas)
    {
        foreach (IGrouping<string, SchemaModel> group in schemas.GroupBy(
                     static schema => schema.ProviderNamespace + "." + schema.ProviderName,
                     StringComparer.Ordinal))
        {
            SchemaModel[] collisions = group
                .OrderBy(static schema => GeneratorUtilities.GetMetadataName(schema.ItemType), StringComparer.Ordinal)
                .ToArray();
            if (collisions.Length < 2)
            {
                continue;
            }

            for (int i = 0; i < collisions.Length; i++)
            {
                collisions[i].ProviderName += "_" + StableHash(GeneratorUtilities.GetMetadataName(collisions[i].ItemType));
            }
        }
    }

    private static string StableHash(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619;
            }

            return hash.ToString("x8");
        }
    }

    private static bool NamespaceMatches(INamedTypeSymbol type, string target, bool includeNested)
    {
        string actual = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        return string.Equals(actual, target, StringComparison.Ordinal) ||
               (includeNested && actual.StartsWith(target + ".", StringComparison.Ordinal));
    }

    private static bool IsEligibleItemType(INamedTypeSymbol type)
    {
        return (type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct) &&
               !type.IsStatic &&
               type.TypeParameters.Length == 0 &&
               GeneratorUtilities.IsAccessibleFromGeneratedCode(type);
    }

    private static bool IsAttribute(AttributeData attribute, string metadataName)
    {
        return attribute.AttributeClass != null &&
               string.Equals(GeneratorUtilities.GetMetadataName(attribute.AttributeClass), metadataName, StringComparison.Ordinal);
    }

    private static INamedTypeSymbol? GetConstructorType(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as INamedTypeSymbol
            : null;
    }

    private static string? GetConstructorString(AttributeData attribute, int index)
    {
        return attribute.ConstructorArguments.Length > index
            ? attribute.ConstructorArguments[index].Value as string
            : null;
    }

    private static int GetEnumValue(Dictionary<string, TypedConstant> arguments, string name, int fallback)
    {
        return arguments.TryGetValue(name, out TypedConstant value) && value.Value is int number ? number : fallback;
    }

    private static Location GetLocation(AttributeData attribute)
    {
        return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
    }

    private sealed class PendingViewModel
    {
        public INamedTypeSymbol ViewModelType { get; set; } = null!;

        public INamedTypeSymbol ItemType { get; set; } = null!;

        public string ColumnDefinitionsPropertyName { get; set; } = "ColumnDefinitions";

        public string SchemaPropertyName { get; set; } = "DataGridSchema";

        public string FastPathOptionsPropertyName { get; set; } = "FastPathOptions";

        public Location Location { get; set; } = Location.None;
    }
}
