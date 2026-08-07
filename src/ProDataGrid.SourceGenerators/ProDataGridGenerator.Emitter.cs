// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace ProDataGrid.SourceGenerators;

internal static class Emitter
{
    public static IEnumerable<GeneratedSource> Emit(GenerationModel model, CancellationToken cancellationToken)
    {
        foreach (SchemaModel schema in model.Schemas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (schema.Columns.Length == 0 && schema.ImplementationType == null)
            {
                continue;
            }

            yield return new GeneratedSource(
                CreateHintName(schema.ProviderNamespace, schema.ProviderName, "Schema"),
                EmitSchema(schema));
        }

        foreach (ViewModelModel viewModel in model.ViewModels)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!viewModel.GenerateColumnDefinitionsProperty &&
                !viewModel.GenerateSchemaProperty &&
                !viewModel.GenerateFastPathOptionsProperty)
            {
                continue;
            }

            yield return new GeneratedSource(
                CreateHintName(
                    viewModel.ViewModelType.ContainingNamespace?.ToDisplayString() ?? string.Empty,
                    GeneratorUtilities.GetMetadataName(viewModel.ViewModelType),
                    "ViewModel"),
                EmitViewModel(viewModel));
        }

        foreach (ViewModelViewModel view in model.Views)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new GeneratedSource(
                CreateHintName(view.ViewNamespace, view.ViewName, "View"),
                EmitView(view));
        }
    }

    private static string EmitSchema(SchemaModel schema)
    {
        string itemType = schema.ItemType.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat);
        string accessibility = IsPubliclyAccessible(schema.ItemType) ? "public" : "internal";
        var builder = new StringBuilder(16384);
        AppendHeader(builder);
        OpenNamespace(builder, schema.ProviderNamespace);
        builder.Append("    ").Append(accessibility).Append(" sealed class ").Append(schema.ProviderName)
            .Append(" : global::Avalonia.Controls.IDataGridGeneratedSchema<").Append(itemType).AppendLine(">")
            .AppendLine("    {")
            .Append("        public static ").Append(schema.ProviderName).AppendLine(" Instance { get; } = new();")
            .AppendLine();

        if (schema.ImplementationType != null)
        {
            EmitImplementationForwarder(builder, schema, itemType);
        }
        else
        {
            EmitGeneratedSchemaBody(builder, schema, itemType);
        }

        builder.AppendLine("    }");
        CloseNamespace(builder, schema.ProviderNamespace);
        return builder.ToString();
    }

    private static void EmitImplementationForwarder(StringBuilder builder, SchemaModel schema, string itemType)
    {
        string implementationType = schema.ImplementationType!.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat);
        builder.Append("        private readonly global::Avalonia.Controls.IDataGridGeneratedSchema<")
            .Append(itemType).Append("> _implementation = new ").Append(implementationType).AppendLine("();")
            .AppendLine()
            .Append("        private ").Append(schema.ProviderName).AppendLine("()")
            .AppendLine("        {")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        public global::Avalonia.Controls.DataGridColumnDefinitionList CreateColumnDefinitions()")
            .AppendLine("            => _implementation.CreateColumnDefinitions();")
            .AppendLine()
            .Append("        public global::System.Collections.Generic.IComparer<").Append(itemType)
            .AppendLine("> CreateSortComparer(global::System.Collections.Generic.IReadOnlyList<global::Avalonia.Controls.DataGridSorting.SortingDescriptor> descriptors)")
            .AppendLine("            => _implementation.CreateSortComparer(descriptors);")
            .AppendLine()
            .Append("        public global::System.Func<").Append(itemType)
            .AppendLine(", bool> CreateFilterPredicate(global::System.Collections.Generic.IReadOnlyList<global::Avalonia.Controls.DataGridFiltering.FilteringDescriptor> descriptors)")
            .AppendLine("            => _implementation.CreateFilterPredicate(descriptors);")
            .AppendLine()
            .Append("        public global::System.Func<").Append(itemType)
            .AppendLine(", bool> CreateSearchPredicate(global::System.Collections.Generic.IReadOnlyList<global::Avalonia.Controls.DataGridSearching.SearchDescriptor> descriptors)")
            .AppendLine("            => _implementation.CreateSearchPredicate(descriptors);")
            .AppendLine()
            .AppendLine("        public global::Avalonia.Controls.DataGridFastPathOptions CreateFastPathOptions()")
            .AppendLine("            => _implementation.CreateFastPathOptions();");
    }

    private static void EmitGeneratedSchemaBody(StringBuilder builder, SchemaModel schema, string itemType)
    {
        foreach (ColumnModel column in schema.Columns)
        {
            EmitAccessorFields(builder, schema, column, itemType);
        }

        builder.AppendLine("        private static readonly global::Avalonia.Controls.DataGridGeneratedDataOperations<" + itemType + "> s_operations =")
            .AppendLine("            new global::Avalonia.Controls.DataGridGeneratedDataOperations<" + itemType + ">(")
            .AppendLine("                new global::Avalonia.Controls.DataGridColumnAccessorRegistration[]")
            .AppendLine("                {");
        foreach (ColumnModel column in schema.Columns)
        {
            string fieldName = GetFieldName(column.Property);
            builder.Append("                    new global::Avalonia.Controls.DataGridColumnAccessorRegistration(")
                .Append(GeneratorUtilities.EscapeString(column.ColumnKey)).Append(", ")
                .Append(GeneratorUtilities.EscapeString(column.Property.Name)).Append(", ")
                .Append(fieldName).Append("Accessor, ")
                .Append(column.IsSearchable ? "true" : "false").AppendLine("),");
        }

        builder.AppendLine("                });")
            .AppendLine()
            .Append("        private ").Append(schema.ProviderName).AppendLine("()")
            .AppendLine("        {")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        public global::Avalonia.Controls.DataGridColumnDefinitionList CreateColumnDefinitions()")
            .AppendLine("        {")
            .Append("            var builder = global::Avalonia.Controls.DataGridColumnDefinitionBuilder.For<")
            .Append(itemType).AppendLine(">();")
            .AppendLine("            var columns = new global::Avalonia.Controls.DataGridColumnDefinitionList();");
        foreach (ColumnModel column in schema.Columns)
        {
            builder.Append("            columns.Add(Create").Append(GetMethodSuffix(column.Property)).AppendLine("Column(builder));");
        }

        if (!string.IsNullOrEmpty(schema.ConfigureMethod))
        {
            builder.Append("            ").Append(itemType).Append('.').Append(GeneratorUtilities.EscapeIdentifier(schema.ConfigureMethod!))
                .AppendLine("(columns);");
        }

        builder.AppendLine("            return columns;")
            .AppendLine("        }")
            .AppendLine();

        foreach (ColumnModel column in schema.Columns)
        {
            EmitColumnFactory(builder, schema, column, itemType);
        }

        builder.Append("        public global::System.Collections.Generic.IComparer<").Append(itemType)
            .AppendLine("> CreateSortComparer(global::System.Collections.Generic.IReadOnlyList<global::Avalonia.Controls.DataGridSorting.SortingDescriptor> descriptors)")
            .AppendLine("            => s_operations.CreateSortComparer(descriptors);")
            .AppendLine()
            .Append("        public global::System.Func<").Append(itemType)
            .AppendLine(", bool> CreateFilterPredicate(global::System.Collections.Generic.IReadOnlyList<global::Avalonia.Controls.DataGridFiltering.FilteringDescriptor> descriptors)")
            .AppendLine("            => s_operations.CreateFilterPredicate(descriptors);")
            .AppendLine()
            .Append("        public global::System.Func<").Append(itemType)
            .AppendLine(", bool> CreateSearchPredicate(global::System.Collections.Generic.IReadOnlyList<global::Avalonia.Controls.DataGridSearching.SearchDescriptor> descriptors)")
            .AppendLine("            => s_operations.CreateSearchPredicate(descriptors);")
            .AppendLine()
            .AppendLine("        public global::Avalonia.Controls.DataGridFastPathOptions CreateFastPathOptions()")
            .AppendLine("        {")
            .AppendLine("            return new global::Avalonia.Controls.DataGridFastPathOptions")
            .AppendLine("            {")
            .Append("                UseAccessorsOnly = ").Append(schema.Strict ? "true" : "false").AppendLine(",")
            .Append("                ThrowOnMissingAccessor = ").Append(schema.Strict ? "true" : "false").AppendLine(",")
            .AppendLine("                EnableHighPerformanceSearching = true,")
            .Append("                HighPerformanceSearchTrackItemChanges = ").Append(schema.Streaming ? "false" : "true").AppendLine()
            .AppendLine("            };")
            .AppendLine("        }");
    }

    private static void EmitAccessorFields(StringBuilder builder, SchemaModel schema, ColumnModel column, string itemType)
    {
        IPropertySymbol property = column.Property;
        string valueType = property.Type.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat);
        string runtimeValueType = UnwrapNullable(property.Type).ToDisplayString(GeneratorUtilities.FullyQualifiedFormat);
        string propertyName = GeneratorUtilities.EscapeIdentifier(property.Name);
        string fieldName = GetFieldName(property);
        bool canWrite = CanWrite(property);

        builder.Append("        private static readonly global::Avalonia.Data.Core.IPropertyInfo ")
            .Append(fieldName).AppendLine("Property =")
            .AppendLine("            new global::Avalonia.Data.Core.ClrPropertyInfo(")
            .Append("                ").Append(GeneratorUtilities.EscapeString(property.Name)).AppendLine(",")
            .Append("                static target => target is ").Append(itemType).Append(" item ? item.")
            .Append(propertyName).Append(" : default(").Append(valueType).AppendLine("),");
        if (canWrite)
        {
            builder.AppendLine("                static (target, value) =>")
                .AppendLine("                {")
                .Append("                    if (target is ").Append(itemType).AppendLine(" item)")
                .AppendLine("                    {")
                .Append("                        item.").Append(propertyName).Append(" = value is null ? default! : (")
                .Append(valueType).AppendLine(")value;")
                .AppendLine("                    }")
                .AppendLine("                },");
        }
        else
        {
            builder.AppendLine("                setter: null,");
        }

        builder.Append("                typeof(").Append(runtimeValueType).AppendLine("));")
            .AppendLine()
            .Append("        private static readonly global::Avalonia.Controls.DataGridColumnValueAccessor<")
            .Append(itemType).Append(", ").Append(valueType).Append("> ").Append(fieldName).AppendLine("Accessor =")
            .Append("            new global::Avalonia.Controls.DataGridColumnValueAccessor<")
            .Append(itemType).Append(", ").Append(valueType).AppendLine(">(")
            .Append("                static item => item.").Append(propertyName);
        if (canWrite)
        {
            builder.AppendLine(",")
                .Append("                static (item, value) => item.").Append(propertyName).AppendLine(" = value);");
        }
        else
        {
            builder.AppendLine(");");
        }

        builder.AppendLine();
    }

    private static void EmitColumnFactory(StringBuilder builder, SchemaModel schema, ColumnModel column, string itemType)
    {
        string definitionTypeName = GetDefinitionTypeName(column.Kind);
        string definitionType = "global::Avalonia.Controls." + definitionTypeName;
        string propertyName = GeneratorUtilities.EscapeIdentifier(column.Property.Name);
        string valueType = column.Property.Type.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat);
        string runtimeValueType = UnwrapNullable(column.Property.Type).ToDisplayString(GeneratorUtilities.FullyQualifiedFormat);
        string fieldName = GetFieldName(column.Property);
        string methodSuffix = GetMethodSuffix(column.Property);
        bool canWrite = CanWrite(column.Property);

        builder.Append("        private static global::Avalonia.Controls.DataGridColumnDefinition Create")
            .Append(methodSuffix).Append("Column(global::Avalonia.Controls.DataGridColumnDefinitionBuilder<")
            .Append(itemType).AppendLine("> builder)")
            .AppendLine("        {")
            .Append("            ").Append(definitionType).Append(" column = ");

        if (!string.IsNullOrEmpty(column.FactoryMethod))
        {
            builder.Append('(').Append(definitionType).Append(')').Append(itemType).Append('.')
                .Append(GeneratorUtilities.EscapeIdentifier(column.FactoryMethod!)).AppendLine("();");
        }
        else
        {
            EmitBuilderCall(builder, column, itemType, valueType, propertyName, fieldName, canWrite);
        }

        builder.Append("            column.ColumnKey = ").Append(GeneratorUtilities.EscapeString(column.ColumnKey)).AppendLine(";")
            .Append("            column.SortMemberPath = ")
            .Append(GeneratorUtilities.EscapeString(GetStringOption(column.Options, "SortMemberPath") ?? column.Property.Name))
            .AppendLine(";")
            .Append("            column.ValueAccessor = ").Append(fieldName).AppendLine("Accessor;")
            .Append("            column.ValueType = typeof(").Append(runtimeValueType).AppendLine(");");

        EmitCommonOptions(builder, column);
        EmitKindOptions(builder, column, itemType);

        if (!string.IsNullOrEmpty(column.ConfigureMethod))
        {
            builder.Append("            ").Append(itemType).Append('.').Append(GeneratorUtilities.EscapeIdentifier(column.ConfigureMethod!))
                .AppendLine("(column);");
        }

        builder.AppendLine("            return column;")
            .AppendLine("        }")
            .AppendLine();
    }

    private static void EmitBuilderCall(
        StringBuilder builder,
        ColumnModel column,
        string itemType,
        string valueType,
        string propertyName,
        string fieldName,
        bool canWrite)
    {
        string header = GeneratorUtilities.EscapeString(column.Header);
        switch (column.Kind)
        {
            case "Template":
                builder.Append("builder.Template(").Append(header).Append(", ")
                    .Append(GeneratorUtilities.EscapeString(GetStringOption(column.Options, "TemplateKey") ?? string.Empty))
                    .AppendLine(");");
                return;
            case "Button":
                builder.Append("builder.Button(").Append(header).Append(", ")
                    .Append(GeneratorUtilities.EscapeString(GetStringOption(column.Options, "Content")))
                    .AppendLine(");");
                return;
            case "Formula":
                builder.Append("builder.Formula(").Append(header).Append(", ")
                    .Append(GeneratorUtilities.EscapeString(GetStringOption(column.Options, "Formula") ?? string.Empty)).Append(", ")
                    .Append(GeneratorUtilities.EscapeString(GetStringOption(column.Options, "FormulaName")))
                    .AppendLine(");");
                return;
        }

        string builderMethod = column.Kind;
        builder.Append("builder.").Append(builderMethod).Append('<').Append(valueType).AppendLine(">(")
            .Append("                ").Append(header).AppendLine(",")
            .Append("                ").Append(fieldName).AppendLine("Property,")
            .Append("                static item => item.").Append(propertyName).AppendLine(",");
        if (canWrite)
        {
            builder.Append("                static (item, value) => item.").Append(propertyName).AppendLine(" = value);");
        }
        else
        {
            builder.AppendLine("                setter: null);");
        }
    }

    private static void EmitCommonOptions(StringBuilder builder, ColumnModel column)
    {
        EmitStringAssignment(builder, column.Options, "HeaderTemplateKey");
        EmitStringAssignment(builder, column.Options, "HeaderThemeKey");
        EmitStringAssignment(builder, column.Options, "CellThemeKey");
        EmitStringAssignment(builder, column.Options, "SummaryCellThemeKey");
        EmitStringAssignment(builder, column.Options, "FilterThemeKey");
        EmitStringAssignment(builder, column.Options, "FilterFlyoutKey");
        EmitStringAssignment(builder, column.Options, "WidthSharingGroup");
        EmitDoubleAssignment(builder, column.Options, "MinWidth");
        EmitDoubleAssignment(builder, column.Options, "MaxWidth");
        EmitBooleanAssignment(builder, column.Options, "CanUserSort");
        EmitBooleanAssignment(builder, column.Options, "CanUserHide");
        EmitBooleanAssignment(builder, column.Options, "CanUserResize");
        EmitBooleanAssignment(builder, column.Options, "CanUserReorder");
        EmitBooleanAssignment(builder, column.Options, "IsReadOnly");
        EmitBooleanAssignment(builder, column.Options, "IsVisible");
        EmitBooleanAssignment(builder, column.Options, "ShowFilterButton");

        if (column.Options.TryGetValue("Width", out TypedConstant widthConstant) && widthConstant.Value is string width)
        {
            builder.Append("            column.Width = ").Append(EmitWidth(width)).AppendLine(";");
        }

        if (column.Options.ContainsKey("IsSearchable") || column.Options.ContainsKey("SearchMemberPath"))
        {
            builder.AppendLine("            column.Options = new global::Avalonia.Controls.DataGridColumnDefinitionOptions")
                .AppendLine("            {")
                .Append("                IsSearchable = ").Append(column.IsSearchable ? "true" : "false").AppendLine(",");
            string? searchPath = GetStringOption(column.Options, "SearchMemberPath");
            if (searchPath != null)
            {
                builder.Append("                SearchMemberPath = ").Append(GeneratorUtilities.EscapeString(searchPath)).AppendLine(",");
            }

            builder.AppendLine("            };");
        }
    }

    private static void EmitKindOptions(StringBuilder builder, ColumnModel column, string itemType)
    {
        string? format = GetStringOption(column.Options, "FormatString");
        string? watermark = GetStringOption(column.Options, "Watermark");
        switch (column.Kind)
        {
            case "Numeric":
                EmitOptionalString(builder, "FormatString", format);
                EmitDecimalAssignment(builder, column.Options, "Minimum");
                EmitDecimalAssignment(builder, column.Options, "Maximum");
                EmitDecimalAssignment(builder, column.Options, "Increment");
                EmitOptionalString(builder, "Watermark", watermark);
                break;
            case "ProgressBar":
                EmitDoubleAssignment(builder, column.Options, "Minimum");
                EmitDoubleAssignment(builder, column.Options, "Maximum");
                EmitOptionalString(builder, "ProgressTextFormat", format);
                break;
            case "Slider":
                EmitDoubleAssignment(builder, column.Options, "Minimum");
                EmitDoubleAssignment(builder, column.Options, "Maximum");
                if (column.Options.TryGetValue("Increment", out TypedConstant increment) && increment.Value is double incrementValue)
                {
                    builder.Append("            column.SmallChange = ").Append(GeneratorUtilities.FormatDouble(incrementValue)).AppendLine(";");
                }
                EmitOptionalString(builder, "ValueTextFormat", format);
                break;
            case "TimePicker":
                EmitOptionalString(builder, "FormatString", format);
                break;
            case "MaskedText":
                EmitOptionalString(builder, "Mask", GetStringOption(column.Options, "Mask"));
                EmitOptionalString(builder, "Watermark", watermark);
                break;
            case "Text":
            case "Hyperlink":
            case "Image":
            case "DatePicker":
                EmitOptionalString(builder, "Watermark", watermark);
                if (format != null && (column.Kind == "Text" || column.Kind == "Hyperlink" || column.Kind == "Image" || column.Kind == "DatePicker"))
                {
                    builder.Append("            column.Binding.StringFormat = ").Append(GeneratorUtilities.EscapeString(format)).AppendLine(";");
                }
                break;
            case "CheckBox":
            case "ToggleButton":
            case "ToggleSwitch":
                EmitBooleanAssignment(builder, column.Options, "IsThreeState");
                EmitToggleContentOptions(builder, column);
                break;
            case "AutoComplete":
                EmitOptionalString(builder, "Watermark", watermark);
                EmitItemsSource(builder, column, itemType);
                break;
            case "ComboBoxSelectedItem":
            case "ComboBoxSelectedValue":
            case "ComboBoxText":
                EmitBooleanAssignment(builder, column.Options, "IsEditable");
                EmitOptionalString(builder, "DisplayMemberPath", GetStringOption(column.Options, "DisplayMemberPath"));
                EmitOptionalString(builder, "SelectedValuePath", GetStringOption(column.Options, "SelectedValuePath"));
                EmitItemsSource(builder, column, itemType);
                if (!column.Options.ContainsKey("ItemsSourceMember") && UnwrapNullable(column.Property.Type).TypeKind == TypeKind.Enum)
                {
                    string enumType = UnwrapNullable(column.Property.Type).ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat);
                    builder.Append("            column.ItemsSource = global::System.Enum.GetValues<").Append(enumType).AppendLine(">();");
                }
                break;
            case "Template":
                EmitOptionalString(builder, "CellEditingTemplateKey", GetStringOption(column.Options, "EditingTemplateKey"));
                break;
            case "Formula":
                break;
            case "Button":
                break;
            case "Hierarchical":
                EmitOptionalString(builder, "CellTemplateKey", GetStringOption(column.Options, "TemplateKey"));
                break;
        }
    }

    private static void EmitToggleContentOptions(StringBuilder builder, ColumnModel column)
    {
        string? content = GetStringOption(column.Options, "Content");
        if (content == null)
        {
            return;
        }

        if (column.Kind == "ToggleSwitch")
        {
            builder.Append("            column.OnContent = ").Append(GeneratorUtilities.EscapeString(content)).AppendLine(";");
        }
        else if (column.Kind == "ToggleButton")
        {
            builder.Append("            column.Content = ").Append(GeneratorUtilities.EscapeString(content)).AppendLine(";");
        }
    }

    private static void EmitItemsSource(StringBuilder builder, ColumnModel column, string itemType)
    {
        string? member = GetStringOption(column.Options, "ItemsSourceMember");
        if (!string.IsNullOrEmpty(member))
        {
            builder.Append("            column.ItemsSource = ").Append(itemType).Append('.')
                .Append(GeneratorUtilities.EscapeIdentifier(member!)).AppendLine(";");
        }
    }

    private static string EmitViewModel(ViewModelModel model)
    {
        string namespaceName = model.ViewModelType.ContainingNamespace?.IsGlobalNamespace == false
            ? model.ViewModelType.ContainingNamespace.ToDisplayString()
            : string.Empty;
        string itemType = model.Schema.ItemType.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat);
        string providerType = string.IsNullOrEmpty(model.Schema.ProviderNamespace)
            ? "global::" + model.Schema.ProviderName
            : "global::" + model.Schema.ProviderNamespace + "." + model.Schema.ProviderName;
        var builder = new StringBuilder(4096);
        AppendHeader(builder);
        OpenNamespace(builder, namespaceName);

        INamedTypeSymbol[] chain = GetContainingTypeChain(model.ViewModelType);
        int indent = 1;
        foreach (INamedTypeSymbol type in chain)
        {
            builder.Append(' ', indent * 4)
                .Append(GetAccessibility(type)).Append(" partial ").Append(GetTypeKeyword(type)).Append(' ')
                .Append(GeneratorUtilities.EscapeIdentifier(type.Name));
            if (type.TypeParameters.Length > 0)
            {
                builder.Append('<').Append(string.Join(", ", type.TypeParameters.Select(static parameter => parameter.Name))).Append('>');
            }

            builder.AppendLine()
                .Append(' ', indent * 4).AppendLine("{");
            indent++;
        }

        string prefix = new string(' ', indent * 4);
        if (model.GenerateSchemaProperty)
        {
            builder.Append(prefix).Append("public global::Avalonia.Controls.IDataGridGeneratedSchema<")
                .Append(itemType).Append("> ").Append(GeneratorUtilities.EscapeIdentifier(model.SchemaPropertyName))
                .Append(" { get; } = ").Append(providerType).AppendLine(".Instance;")
                .AppendLine();
        }

        if (model.GenerateColumnDefinitionsProperty)
        {
            builder.Append(prefix).Append("public global::Avalonia.Controls.DataGridColumnDefinitionList ")
                .Append(GeneratorUtilities.EscapeIdentifier(model.ColumnDefinitionsPropertyName))
                .Append(" { get; } = ").Append(providerType).AppendLine(".Instance.CreateColumnDefinitions();")
                .AppendLine();
        }

        if (model.GenerateFastPathOptionsProperty)
        {
            builder.Append(prefix).Append("public global::Avalonia.Controls.DataGridFastPathOptions ")
                .Append(GeneratorUtilities.EscapeIdentifier(model.FastPathOptionsPropertyName))
                .Append(" { get; } = ").Append(providerType).AppendLine(".Instance.CreateFastPathOptions();");
        }

        for (int i = chain.Length - 1; i >= 0; i--)
        {
            indent--;
            builder.Append(' ', indent * 4).AppendLine("}");
        }

        CloseNamespace(builder, namespaceName);
        return builder.ToString();
    }

    private static string EmitView(ViewModelViewModel model)
    {
        string viewModelType = model.ViewModelType.ToDisplayString(GeneratorUtilities.FullyQualifiedNullableFormat);
        string accessibility = IsPubliclyAccessible(model.ViewModelType) ? "public" : "internal";
        string baseType = ViewGenerationStrategyRegistry.Get(model.Framework).GetBaseType(model);
        var builder = new StringBuilder(12288);
        AppendHeader(builder);
        OpenNamespace(builder, model.ViewNamespace);
        builder.Append("    ").Append(accessibility).Append(" class ")
            .Append(model.ViewName).Append(" : ").Append(baseType).AppendLine()
            .AppendLine("    {");

        EmitViewPropertyInfo(builder, model.Items, viewModelType, "Items");
        EmitViewPropertyInfo(builder, model.ColumnDefinitions, viewModelType, "ColumnDefinitions");
        EmitViewPropertyInfo(builder, model.FastPathOptions, viewModelType, "FastPathOptions");
        if (model.SortingModel != null)
        {
            EmitViewPropertyInfo(builder, model.SortingModel, viewModelType, "SortingModel");
        }
        if (model.FilteringModel != null)
        {
            EmitViewPropertyInfo(builder, model.FilteringModel, viewModelType, "FilteringModel");
        }
        if (model.SearchModel != null)
        {
            EmitViewPropertyInfo(builder, model.SearchModel, viewModelType, "SearchModel");
        }
        if (model.SearchText != null)
        {
            EmitViewPropertyInfo(builder, model.SearchText, viewModelType, "SearchText");
        }

        builder.Append("        public ").Append(model.ViewName).AppendLine("()")
            .AppendLine("        {")
            .AppendLine("            Content = CreateGeneratedContent();")
            .AppendLine("        }")
            .AppendLine()
            .Append("        public ").Append(model.ViewName).Append('(').Append(viewModelType).AppendLine(" viewModel)")
            .AppendLine("            : this()")
            .AppendLine("        {")
            .AppendLine("            DataContext = viewModel ?? throw new global::System.ArgumentNullException(nameof(viewModel));")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        protected virtual global::Avalonia.Controls.Control CreateGeneratedContent()")
            .AppendLine("        {")
            .AppendLine("            var header = new global::Avalonia.Controls.StackPanel")
            .AppendLine("            {")
            .AppendLine("                Spacing = 6d,")
            .AppendLine("                Children =")
            .AppendLine("                {")
            .AppendLine("                    new global::Avalonia.Controls.TextBlock")
            .AppendLine("                    {")
            .Append("                        Text = ").Append(GeneratorUtilities.EscapeString(model.Title)).AppendLine(",")
            .AppendLine("                        FontSize = 22d,")
            .AppendLine("                        FontWeight = global::Avalonia.Media.FontWeight.SemiBold")
            .AppendLine("                    }")
            .AppendLine("                }")
            .AppendLine("            };");

        if (model.SearchText != null)
        {
            builder.AppendLine("            var searchBox = new global::Avalonia.Controls.TextBox")
                .AppendLine("            {")
                .AppendLine("                Name = \"GeneratedSearchBox\",")
                .AppendLine("                Watermark = \"Search\",")
                .AppendLine("                Width = 280d,")
                .AppendLine("                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Left")
                .AppendLine("            };")
                .AppendLine("            searchBox[!global::Avalonia.Controls.TextBox.TextProperty] = CreateBinding(s_searchTextProperty, global::Avalonia.Data.BindingMode.TwoWay);")
                .AppendLine("            header.Children.Add(searchBox);");
        }

        builder.AppendLine("            var dataGrid = CreateGeneratedDataGrid();")
            .AppendLine("            var layout = new global::Avalonia.Controls.Grid")
            .AppendLine("            {")
            .AppendLine("                Margin = new global::Avalonia.Thickness(12d),")
            .AppendLine("                RowDefinitions = new global::Avalonia.Controls.RowDefinitions(\"Auto,*\"),")
            .AppendLine("                RowSpacing = 8d")
            .AppendLine("            };")
            .AppendLine("            global::Avalonia.Controls.Grid.SetRow(dataGrid, 1);")
            .AppendLine("            layout.Children.Add(header);")
            .AppendLine("            layout.Children.Add(dataGrid);")
            .AppendLine("            return layout;")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        protected virtual global::Avalonia.Controls.DataGrid CreateGeneratedDataGrid()")
            .AppendLine("        {")
            .AppendLine("            var dataGrid = new global::Avalonia.Controls.DataGrid")
            .AppendLine("            {")
            .AppendLine("                Name = \"GeneratedDataGrid\",")
            .AppendLine("                AutoGenerateColumns = false,")
            .AppendLine("                CanUserSortColumns = true,")
            .AppendLine("                GridLinesVisibility = global::Avalonia.Controls.DataGridGridLinesVisibility.Horizontal")
            .AppendLine("            };")
            .AppendLine("            dataGrid[!global::Avalonia.Controls.DataGrid.ItemsSourceProperty] = CreateBinding(s_itemsProperty, global::Avalonia.Data.BindingMode.OneWay);")
            .AppendLine("            dataGrid[!global::Avalonia.Controls.DataGrid.ColumnDefinitionsSourceProperty] = CreateBinding(s_columnDefinitionsProperty, global::Avalonia.Data.BindingMode.OneWay);")
            .AppendLine("            dataGrid[!global::Avalonia.Controls.DataGrid.FastPathOptionsProperty] = CreateBinding(s_fastPathOptionsProperty, global::Avalonia.Data.BindingMode.OneWay);");

        EmitOptionalGridBinding(builder, model.SortingModel, "SortingModel", "s_sortingModelProperty");
        EmitOptionalGridBinding(builder, model.FilteringModel, "FilteringModel", "s_filteringModelProperty");
        EmitOptionalGridBinding(builder, model.SearchModel, "SearchModel", "s_searchModelProperty");

        builder.AppendLine("            ConfigureGeneratedDataGrid(dataGrid);")
            .AppendLine("            return dataGrid;")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        protected virtual void ConfigureGeneratedDataGrid(global::Avalonia.Controls.DataGrid dataGrid)")
            .AppendLine("        {")
            .AppendLine("        }")
            .AppendLine()
            .AppendLine("        private static global::Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindingExtension CreateBinding(")
            .AppendLine("            global::Avalonia.Data.Core.IPropertyInfo property,")
            .AppendLine("            global::Avalonia.Data.BindingMode mode)")
            .AppendLine("        {")
            .AppendLine("            return new global::Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindingExtension")
            .AppendLine("            {")
            .Append("                DataType = typeof(").Append(viewModelType).AppendLine("),")
            .AppendLine("                Mode = mode,")
            .AppendLine("                Path = new global::Avalonia.Data.CompiledBindingPathBuilder()")
            .AppendLine("                    .Property(property, global::Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings.PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)")
            .AppendLine("                    .Build()")
            .AppendLine("            };")
            .AppendLine("        }")
            .AppendLine("    }");
        CloseNamespace(builder, model.ViewNamespace);
        return builder.ToString();
    }

    private static void EmitViewPropertyInfo(
        StringBuilder builder,
        ViewBindingModel binding,
        string viewModelType,
        string role)
    {
        string fieldName = "s_" + char.ToLowerInvariant(role[0]) + role.Substring(1) + "Property";
        string propertyName = GeneratorUtilities.EscapeIdentifier(binding.PropertyName);
        builder.Append("        private static readonly global::Avalonia.Data.Core.IPropertyInfo ")
            .Append(fieldName).AppendLine(" =")
            .AppendLine("            new global::Avalonia.Data.Core.ClrPropertyInfo(")
            .Append("                ").Append(GeneratorUtilities.EscapeString(binding.PropertyName)).AppendLine(",")
            .Append("                static target => target is ").Append(viewModelType).Append(" viewModel ? viewModel.")
            .Append(propertyName).Append(" : default(").Append(binding.PropertyType).AppendLine("),");
        if (binding.CanWrite)
        {
            builder.AppendLine("                static (target, value) =>")
                .AppendLine("                {")
                .Append("                    if (target is ").Append(viewModelType).AppendLine(" viewModel)")
                .AppendLine("                    {")
                .Append("                        viewModel.").Append(propertyName).Append(" = value is null ? default! : (")
                .Append(binding.PropertyType).AppendLine(")value;")
                .AppendLine("                    }")
                .AppendLine("                },");
        }
        else
        {
            builder.AppendLine("                setter: null,");
        }

        builder.Append("                typeof(").Append(binding.RuntimePropertyType).AppendLine("));")
            .AppendLine();
    }

    private static void EmitOptionalGridBinding(
        StringBuilder builder,
        ViewBindingModel? binding,
        string propertyName,
        string fieldName)
    {
        if (binding == null)
        {
            return;
        }

        builder.Append("            dataGrid[!global::Avalonia.Controls.DataGrid.").Append(propertyName)
            .Append("Property] = CreateBinding(").Append(fieldName)
            .AppendLine(", global::Avalonia.Data.BindingMode.OneWay);");
    }

    private static void EmitStringAssignment(StringBuilder builder, ImmutableDictionary<string, TypedConstant> options, string propertyName)
    {
        if (options.TryGetValue(propertyName, out TypedConstant value) && value.Value is string text)
        {
            builder.Append("            column.").Append(propertyName).Append(" = ")
                .Append(GeneratorUtilities.EscapeString(text)).AppendLine(";");
        }
    }

    private static void EmitOptionalString(StringBuilder builder, string propertyName, string? value)
    {
        if (value != null)
        {
            builder.Append("            column.").Append(propertyName).Append(" = ")
                .Append(GeneratorUtilities.EscapeString(value)).AppendLine(";");
        }
    }

    private static void EmitBooleanAssignment(StringBuilder builder, ImmutableDictionary<string, TypedConstant> options, string propertyName)
    {
        if (options.TryGetValue(propertyName, out TypedConstant value) && value.Value is bool boolean)
        {
            builder.Append("            column.").Append(propertyName).Append(" = ")
                .Append(boolean ? "true" : "false").AppendLine(";");
        }
    }

    private static void EmitDoubleAssignment(StringBuilder builder, ImmutableDictionary<string, TypedConstant> options, string propertyName)
    {
        if (options.TryGetValue(propertyName, out TypedConstant value) && value.Value is double number)
        {
            builder.Append("            column.").Append(propertyName).Append(" = ")
                .Append(GeneratorUtilities.FormatDouble(number)).AppendLine(";");
        }
    }

    private static void EmitDecimalAssignment(StringBuilder builder, ImmutableDictionary<string, TypedConstant> options, string propertyName)
    {
        if (options.TryGetValue(propertyName, out TypedConstant value) && value.Value is double number)
        {
            builder.Append("            column.").Append(propertyName).Append(" = (decimal)")
                .Append(GeneratorUtilities.FormatDouble(number)).AppendLine(";");
        }
    }

    private static string? GetStringOption(ImmutableDictionary<string, TypedConstant> options, string name)
    {
        return options.TryGetValue(name, out TypedConstant value) ? value.Value as string : null;
    }

    private static string EmitWidth(string width)
    {
        string trimmed = width.Trim();
        if (string.Equals(trimmed, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            return "global::Avalonia.Controls.DataGridLength.Auto";
        }

        if (trimmed.EndsWith("*", StringComparison.Ordinal))
        {
            string factorText = trimmed.Substring(0, trimmed.Length - 1);
            double factor = string.IsNullOrWhiteSpace(factorText)
                ? 1d
                : double.TryParse(factorText, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? parsed : 1d;
            return "new global::Avalonia.Controls.DataGridLength(" + GeneratorUtilities.FormatDouble(factor) +
                   ", global::Avalonia.Controls.DataGridLengthUnitType.Star)";
        }

        double pixels = double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedPixels)
            ? parsedPixels
            : 100d;
        return "new global::Avalonia.Controls.DataGridLength(" + GeneratorUtilities.FormatDouble(pixels) +
               ", global::Avalonia.Controls.DataGridLengthUnitType.Pixel)";
    }

    private static bool CanWrite(IPropertySymbol property)
    {
        return property.SetMethod != null &&
               !property.SetMethod.IsInitOnly &&
               GeneratorUtilities.IsAccessibleFromGeneratedCode(property.SetMethod);
    }

    private static string GetDefinitionTypeName(string kind)
    {
        return kind switch
        {
            "Text" => "DataGridTextColumnDefinition",
            "CheckBox" => "DataGridCheckBoxColumnDefinition",
            "Hyperlink" => "DataGridHyperlinkColumnDefinition",
            "Image" => "DataGridImageColumnDefinition",
            "Numeric" => "DataGridNumericColumnDefinition",
            "ProgressBar" => "DataGridProgressBarColumnDefinition",
            "Slider" => "DataGridSliderColumnDefinition",
            "DatePicker" => "DataGridDatePickerColumnDefinition",
            "TimePicker" => "DataGridTimePickerColumnDefinition",
            "MaskedText" => "DataGridMaskedTextColumnDefinition",
            "AutoComplete" => "DataGridAutoCompleteColumnDefinition",
            "ToggleButton" => "DataGridToggleButtonColumnDefinition",
            "ToggleSwitch" => "DataGridToggleSwitchColumnDefinition",
            "Hierarchical" => "DataGridHierarchicalColumnDefinition",
            "CustomDrawing" => "DataGridCustomDrawingColumnDefinition",
            "ComboBoxSelectedItem" or "ComboBoxSelectedValue" or "ComboBoxText" => "DataGridComboBoxColumnDefinition",
            "Template" => "DataGridTemplateColumnDefinition",
            "Button" => "DataGridButtonColumnDefinition",
            "Formula" => "DataGridFormulaColumnDefinition",
            _ => "DataGridTextColumnDefinition"
        };
    }

    private static string GetFieldName(IPropertySymbol property)
    {
        return "s_" + GeneratorUtilities.SanitizeIdentifier(property.Name).TrimStart('@');
    }

    private static string GetMethodSuffix(IPropertySymbol property)
    {
        return GeneratorUtilities.SanitizeIdentifier(property.Name).TrimStart('@');
    }

    private static string CreateHintName(string namespaceName, string name, string suffix)
    {
        string raw = namespaceName + "." + name + "." + suffix;
        var builder = new StringBuilder(raw.Length + 5);
        for (int i = 0; i < raw.Length; i++)
        {
            char value = raw[i];
            builder.Append(char.IsLetterOrDigit(value) || value == '_' ? value : '_');
        }

        return builder.Append(".g.cs").ToString();
    }

    private static bool IsPubliclyAccessible(INamedTypeSymbol type)
    {
        INamedTypeSymbol? current = type;
        while (current != null)
        {
            if (current.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            current = current.ContainingType;
        }

        return true;
    }

    private static INamedTypeSymbol[] GetContainingTypeChain(INamedTypeSymbol type)
    {
        var stack = new Stack<INamedTypeSymbol>();
        INamedTypeSymbol? current = type;
        while (current != null)
        {
            stack.Push(current);
            current = current.ContainingType;
        }

        return stack.ToArray();
    }

    private static string GetAccessibility(INamedTypeSymbol type)
    {
        return type.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            _ => "private"
        };
    }

    private static string GetTypeKeyword(INamedTypeSymbol type)
    {
        if (type.IsRecord)
        {
            return type.TypeKind == TypeKind.Struct ? "record struct" : "record";
        }

        return type.TypeKind == TypeKind.Struct ? "struct" : "class";
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

    private static void AppendHeader(StringBuilder builder)
    {
        builder.AppendLine("// <auto-generated/>")
            .AppendLine("#nullable enable")
            .AppendLine();
    }

    private static void OpenNamespace(StringBuilder builder, string namespaceName)
    {
        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.Append("namespace ").Append(namespaceName).AppendLine()
                .AppendLine("{");
        }
    }

    private static void CloseNamespace(StringBuilder builder, string namespaceName)
    {
        if (!string.IsNullOrEmpty(namespaceName))
        {
            builder.AppendLine("}");
        }
    }
}
