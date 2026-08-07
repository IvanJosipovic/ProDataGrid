// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ProDataGrid.SourceGenerators.UnitTests;

public sealed class ProDataGridGeneratorTests
{
    [Fact]
    public void Property_attribute_generates_schema_and_typed_accessor()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row
            {
                [DataGridColumn(Header = "Display name", Width = "2*")]
                public string Name { get; set; } = string.Empty;
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class RowDataGridSchema", result.CombinedSource);
        Assert.Contains("DataGridColumnValueAccessor<global::Demo.Row, string>", result.CombinedSource);
        Assert.Contains("DataGridGeneratedDistinctValueProvider<global::Demo.Row, string> NameDistinctValues", result.CombinedSource);
        Assert.Contains("CreateNameRemoteDistinctValues", result.CombinedSource);
        Assert.Contains("Display name", result.CombinedSource);
        Assert.Contains("DataGridLengthUnitType.Star", result.CombinedSource);
    }

    [Fact]
    public void Type_attribute_discovers_public_properties_and_default_kinds()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                public string Name { get; set; } = string.Empty;
                public bool Enabled { get; set; }
                public decimal Amount { get; set; }
                public DateTime Date { get; set; }
                public Status Status { get; set; }
            }
            public enum Status { New, Done }
            """);

        AssertNoErrors(result);
        Assert.Contains("builder.Text<string>", result.CombinedSource);
        Assert.Contains("builder.CheckBox<bool>", result.CombinedSource);
        Assert.Contains("builder.Numeric<decimal>", result.CombinedSource);
        Assert.Contains("builder.DatePicker<global::System.DateTime>", result.CombinedSource);
        Assert.Contains("builder.ComboBoxSelectedItem<global::Demo.Status>", result.CombinedSource);
        Assert.Contains("Enum.GetValues<global::Demo.Status>()", result.CombinedSource);
    }

    [Fact]
    public void Assembly_attribute_targets_item_type()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            [assembly: GenerateDataGridColumns(typeof(Demo.Row), ProviderName = "AssemblyColumns")]
            namespace Demo { public sealed class Row { public int Id { get; set; } } }
            """);

        AssertNoErrors(result);
        Assert.Contains("class AssemblyColumns", result.CombinedSource);
    }

    [Fact]
    public void Namespace_attribute_generates_all_matching_models()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            [assembly: GenerateDataGridColumnsForNamespace("Demo.Models")]
            namespace Demo.Models
            {
                public sealed class First { public int Id { get; set; } }
                public sealed class Second { public string Name { get; set; } = ""; }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class FirstDataGridSchema", result.CombinedSource);
        Assert.Contains("class SecondDataGridSchema", result.CombinedSource);
    }

    [Fact]
    public void Assembly_registry_exposes_cross_assembly_manifest_lookup()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            [assembly: GenerateDataGridRegistry(RegistryNamespace = "Demo.Registration", RegistryName = "GridSchemas")]
            namespace Demo.Models
            {
                [GenerateDataGridColumns(SchemaId = "demo/row/v1")]
                public sealed class Row { public int Id { get; set; } }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("namespace Demo.Registration", result.CombinedSource);
        Assert.Contains("public static class GridSchemas", result.CombinedSource);
        Assert.Contains("IReadOnlyList<global::Avalonia.Controls.IDataGridGeneratedSchemaManifestProvider> Schemas", result.CombinedSource);
        Assert.Contains("TryGetSchema(", result.CombinedSource);
        Assert.Contains("itemType == typeof(global::Demo.Models.Row)", result.CombinedSource);
        Assert.Contains("RowDataGridSchema.SchemaId", result.CombinedSource);
    }

    [Fact]
    public void Assembly_registry_emits_optional_microsoft_di_registration_when_available()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System;
            using ProDataGrid.SourceGeneration;
            [assembly: GenerateDataGridRegistry(RegistryNamespace = "Demo.Registration", RegistryName = "GridSchemas")]
            namespace Microsoft.Extensions.DependencyInjection
            {
                public interface IServiceCollection { void Add(ServiceDescriptor descriptor); }
                public sealed class ServiceDescriptor
                {
                    public static ServiceDescriptor Singleton(Type serviceType, object instance) => new();
                }
            }
            namespace Demo.Models
            {
                [GenerateDataGridColumns]
                public sealed class Row { public int Id { get; set; } }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("AddGeneratedProDataGrids", result.CombinedSource);
        Assert.Contains("IDataGridGeneratedSchema<global::Demo.Models.Row>", result.CombinedSource);
        Assert.Contains("ServiceDescriptor.Singleton", result.CombinedSource);
    }

    [Fact]
    public void Partial_view_model_receives_columns_schema_and_fast_options()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridViewModel(typeof(Row), Streaming = true)]
            public sealed partial class RowsViewModel { }
            """);

        AssertNoErrors(result);
        Assert.Contains("partial class RowsViewModel", result.CombinedSource);
        Assert.Contains("DataGridColumnDefinitionList ColumnDefinitions", result.CombinedSource);
        Assert.Contains("IDataGridGeneratedSchema<global::Demo.Row> DataGridSchema", result.CombinedSource);
        Assert.Contains("DataGridFastPathOptions FastPathOptions", result.CombinedSource);
        Assert.Contains("HighPerformanceSearchTrackItemChanges = false", result.CombinedSource);
    }

    [Fact]
    public void Assembly_view_model_attribute_generates_partial_members()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            [assembly: GenerateDataGridViewModel(typeof(Demo.RowsViewModel), typeof(Demo.Row))]
            namespace Demo
            {
                public sealed class Row { public int Id { get; set; } }
                public sealed partial class RowsViewModel { }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("partial class RowsViewModel", result.CombinedSource);
    }

    [Fact]
    public void Namespace_view_model_attribute_infers_items_type()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using ProDataGrid.SourceGeneration;
            [assembly: GenerateDataGridViewModelsForNamespace("Demo.ViewModels")]
            namespace Demo.Models { public sealed class Row { public int Id { get; set; } } }
            namespace Demo.ViewModels
            {
                public sealed partial class RowsViewModel
                {
                    public IReadOnlyList<Demo.Models.Row> Items { get; } = new List<Demo.Models.Row>();
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("IDataGridGeneratedSchema<global::Demo.Models.Row>", result.CombinedSource);
    }

    [Fact]
    public void Ignore_attribute_and_attributed_only_discovery_are_honored()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(Discovery = DataGridColumnDiscovery.AttributedOnly)]
            public sealed class Row
            {
                [DataGridColumn] public int Included { get; set; }
                [DataGridIgnoreColumn] public int Ignored { get; set; }
                public int NotAttributed { get; set; }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("CreateIncludedColumn", result.CombinedSource);
        Assert.DoesNotContain("CreateIgnoredColumn", result.CombinedSource);
        Assert.DoesNotContain("CreateNotAttributedColumn", result.CombinedSource);
    }

    [Fact]
    public void Read_only_property_does_not_generate_setter()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row { public string Display => "value"; }
            """);

        AssertNoErrors(result);
        Assert.Contains("setter: null", result.CombinedSource);
        Assert.Contains("static item => item.Display", result.CombinedSource);
    }

    [Fact]
    public void Configure_and_factory_methods_are_called_directly()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(ConfigureMethod = nameof(ConfigureColumns))]
            public sealed class Row
            {
                [DataGridColumn(Kind = DataGridColumnKind.Text, ConfigureMethod = nameof(ConfigureName))]
                public string Name { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.Button, FactoryMethod = nameof(CreateAction))]
                public string Action { get; } = "Run";

                public static void ConfigureName(DataGridTextColumnDefinition column) => column.Watermark = "name";
                public static DataGridButtonColumnDefinition CreateAction() => new();
                public static void ConfigureColumns(DataGridColumnDefinitionList columns) { }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("global::Demo.Row.ConfigureName(column);", result.CombinedSource);
        Assert.Contains("global::Demo.Row.CreateAction();", result.CombinedSource);
        Assert.Contains("global::Demo.Row.ConfigureColumns(columns);", result.CombinedSource);
    }

    [Fact]
    public void All_column_kinds_emit_builder_or_definition_paths()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(Discovery = DataGridColumnDiscovery.AttributedOnly)]
            public sealed class Row
            {
                [DataGridColumn(Kind = DataGridColumnKind.Text)] public string Text { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.CheckBox)] public bool Check { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.Hyperlink)] public string Link { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.Image)] public object Image { get; set; } = new();
                [DataGridColumn(Kind = DataGridColumnKind.Numeric)] public decimal Number { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.ProgressBar)] public double Progress { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.Slider)] public double Slider { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.DatePicker)] public System.DateTime Date { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.TimePicker)] public System.TimeSpan Time { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.MaskedText, Mask = "000")] public string Masked { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.AutoComplete)] public string Auto { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.ToggleButton)] public bool Toggle { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.ToggleSwitch)] public bool Switch { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.Hierarchical)] public string Tree { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.CustomDrawing)] public string Draw { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.ComboBoxSelectedItem)] public string ComboItem { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.ComboBoxSelectedValue)] public int ComboValue { get; set; }
                [DataGridColumn(Kind = DataGridColumnKind.ComboBoxText)] public string ComboText { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.Template, TemplateKey = "CellTemplate")] public string Template { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.Button, Content = "Run")] public string Button { get; set; } = "";
                [DataGridColumn(Kind = DataGridColumnKind.Formula, Formula = "=A1")] public double Formula { get; set; }
            }
            """);

        AssertNoErrors(result);
        foreach (string marker in new[]
                 {
                     "builder.Text<", "builder.CheckBox<", "builder.Hyperlink<", "builder.Image<", "builder.Numeric<",
                     "builder.ProgressBar<", "builder.Slider<", "builder.DatePicker<", "builder.TimePicker<", "builder.MaskedText<",
                     "builder.AutoComplete<", "builder.ToggleButton<", "builder.ToggleSwitch<", "builder.Hierarchical<",
                     "builder.CustomDrawing<", "builder.ComboBoxSelectedItem<", "builder.ComboBoxSelectedValue<",
                     "builder.ComboBoxText<", "builder.Template(", "builder.Button(", "builder.Formula("
                 })
        {
            Assert.Contains(marker, result.CombinedSource);
        }
    }

    [Fact]
    public void User_defined_schema_implementation_is_forwarded_without_reflection()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System;
            using System.Collections.Generic;
            using Avalonia.Controls;
            using Avalonia.Controls.DataGridFiltering;
            using Avalonia.Controls.DataGridSearching;
            using Avalonia.Controls.DataGridSorting;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(ImplementationType = typeof(CustomSchema), ProviderName = "GeneratedFacade")]
            public sealed class Row { public int Id { get; set; } }
            public sealed class CustomSchema : IDataGridGeneratedSchema<Row>
            {
                public DataGridColumnDefinitionList CreateColumnDefinitions() => new();
                public IComparer<Row> CreateSortComparer(IReadOnlyList<SortingDescriptor> descriptors) => Comparer<Row>.Default;
                public Func<Row, bool> CreateFilterPredicate(IReadOnlyList<FilteringDescriptor> descriptors) => static _ => true;
                public Func<Row, bool> CreateSearchPredicate(IReadOnlyList<SearchDescriptor> descriptors) => static _ => true;
                public DataGridFastPathOptions CreateFastPathOptions() => new();
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class GeneratedFacade", result.CombinedSource);
        Assert.Contains("new global::Demo.CustomSchema()", result.CombinedSource);
        Assert.Contains("_implementation.CreateSearchPredicate(descriptors)", result.CombinedSource);
    }

    [Fact]
    public void Inherited_public_properties_are_included_by_default()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public class BaseRow { public int Id { get; set; } }
            [GenerateDataGridColumns]
            public sealed class Row : BaseRow { public string Name { get; set; } = ""; }
            """);

        AssertNoErrors(result);
        Assert.Contains("CreateIdColumn", result.CombinedSource);
        Assert.Contains("CreateNameColumn", result.CombinedSource);
    }

    [Fact]
    public void Avalonia_view_generation_emits_code_only_layout_and_compiled_binding_indexers()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridViewModel(typeof(Row))]
            [GenerateDataGridView(typeof(Row), ViewName = "RowsPage", ViewNamespace = "Demo.Views", Title = "Rows")]
            public sealed partial class RowsViewModel
            {
                public IReadOnlyList<Row> Items { get; } = new List<Row>();
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class RowsPage : global::Avalonia.Controls.UserControl", result.CombinedSource);
        Assert.Contains("dataGrid[!global::Avalonia.Controls.DataGrid.ItemsSourceProperty]", result.CombinedSource);
        Assert.Contains("CompiledBindingPathBuilder", result.CombinedSource);
        Assert.Contains("protected virtual void ConfigureGeneratedDataGrid", result.CombinedSource);
        Assert.Contains("AutomationProperties.SetAutomationId(dataGrid, GeneratedAutomationId)", result.CombinedSource);
        Assert.Contains("AutomationProperties.SetHeadingLevel(title, 1)", result.CombinedSource);
    }

    [Fact]
    public void Reactive_ui_view_strategy_uses_typed_reactive_user_control()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using ProDataGrid.SourceGeneration;
            namespace ReactiveUI.Avalonia
            {
                public class ReactiveUserControl<T> : global::Avalonia.Controls.UserControl { }
            }
            namespace Demo
            {
                public sealed class Row { public int Id { get; set; } }
                [GenerateDataGridViewModel(typeof(Row))]
                [GenerateDataGridView(typeof(Row), Framework = DataGridViewFramework.ReactiveUI)]
                public sealed partial class RowsViewModel
                {
                    public IReadOnlyList<Row> Items { get; } = new List<Row>();
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains(
            "ReactiveUserControl<global::Demo.RowsViewModel>",
            result.CombinedSource);
    }

    [Fact]
    public void Generated_view_supports_custom_base_and_search_binding()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public class GridViewBase : UserControl { }
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridViewModel(typeof(Row))]
            [GenerateDataGridView(typeof(Row), BaseType = typeof(GridViewBase), SearchTextPropertyName = nameof(Query))]
            public sealed partial class RowsViewModel
            {
                public IReadOnlyList<Row> Items { get; } = new List<Row>();
                public string Query { get; set; } = "";
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class RowsView : global::Demo.GridViewBase", result.CombinedSource);
        Assert.Contains("TextBox.TextProperty", result.CombinedSource);
        Assert.Contains("BindingMode.TwoWay", result.CombinedSource);
    }

    [Fact]
    public void Generated_view_binds_shared_selection_and_emits_state_adapter()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using Avalonia.Controls;
            using Avalonia.Controls.Selection;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { [DataGridKey] public int Id { get; set; } }
            [GenerateDataGridViewModel(typeof(Row))]
            [GenerateDataGridView(
                typeof(Row),
                SelectionModelPropertyName = nameof(Selection),
                StateControllerPropertyName = nameof(GridState))]
            public sealed partial class RowsViewModel
            {
                public IReadOnlyList<Row> Items { get; } = new List<Row>();
                public ISelectionModel Selection { get; } = null!;
                public DataGridGeneratedStateController GridState { get; } = null!;
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("DataGrid.SelectionProperty", result.CombinedSource);
        Assert.Contains("CaptureGeneratedState(", result.CombinedSource);
        Assert.Contains("RestoreGeneratedState(", result.CombinedSource);
        Assert.Contains("GridState).Capture(GeneratedDataGrid", result.CombinedSource);
    }

    [Fact]
    public void Assembly_view_attribute_and_namespace_view_attribute_are_supported()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using ProDataGrid.SourceGeneration;
            [assembly: GenerateDataGridView(typeof(Demo.FirstViewModel), typeof(Demo.Row), ViewName = "FirstGrid")]
            [assembly: GenerateDataGridViewsForNamespace("Demo.Generated")]
            namespace Demo
            {
                public sealed class Row { public int Id { get; set; } }
                [GenerateDataGridViewModel(typeof(Row))]
                public sealed partial class FirstViewModel
                {
                    public IReadOnlyList<Row> Items { get; } = new List<Row>();
                }
            }
            namespace Demo.Generated
            {
                public sealed class Item { public int Id { get; set; } }
                [GenerateDataGridViewModel(typeof(Item))]
                public sealed partial class SecondViewModel
                {
                    public IReadOnlyList<Item> Items { get; } = new List<Item>();
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class FirstGrid", result.CombinedSource);
        Assert.Contains("class SecondView", result.CombinedSource);
    }

    [Fact]
    public void Missing_generated_view_member_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridView(typeof(Row), SearchTextPropertyName = "Missing")]
            public sealed class RowsViewModel { public Row[] Items { get; } = []; }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG012");
    }

    [Fact]
    public void Multiple_view_attributes_generate_independent_framework_views()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using ProDataGrid.SourceGeneration;
            namespace ReactiveUI.Avalonia
            {
                public class ReactiveUserControl<T> : global::Avalonia.Controls.UserControl { }
            }
            namespace Demo
            {
                public sealed class Row { public int Id { get; set; } }
                [GenerateDataGridViewModel(typeof(Row))]
                [GenerateDataGridView(typeof(Row), ViewName = "PlainRowsView")]
                [GenerateDataGridView(typeof(Row), ViewName = "ReactiveRowsView", Framework = DataGridViewFramework.ReactiveUI)]
                public sealed partial class RowsViewModel
                {
                    public IReadOnlyList<Row> Items { get; } = new List<Row>();
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class PlainRowsView", result.CombinedSource);
        Assert.Contains("class ReactiveRowsView", result.CombinedSource);
    }

    [Fact]
    public void Invalid_custom_view_base_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public class NotAControl { }
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridViewModel(typeof(Row))]
            [GenerateDataGridView(typeof(Row), BaseType = typeof(NotAControl))]
            public sealed partial class RowsViewModel { public Row[] Items { get; } = []; }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG013");
    }

    [Fact]
    public void Reactive_source_generated_fields_are_recognized_as_view_binding_members()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System;
            using ProDataGrid.SourceGeneration;
            namespace ReactiveUI.SourceGenerators
            {
                [AttributeUsage(AttributeTargets.Field)]
                public sealed class ReactiveAttribute : Attribute { }
            }
            namespace Demo
            {
                public sealed class Row { public int Id { get; set; } }
                [GenerateDataGridViewModel(typeof(Row))]
                [GenerateDataGridView(typeof(Row), SearchTextPropertyName = "Query")]
                public sealed partial class RowsViewModel
                {
                    public Row[] Items { get; } = [];
                    [ReactiveUI.SourceGenerators.Reactive] private string _query = "";
                }
            }
            """);

        Assert.DoesNotContain(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG012");
        Assert.Contains("viewModel.Query", result.CombinedSource);
    }

    [Fact]
    public void Non_partial_view_model_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridViewModel(typeof(Row))]
            public sealed class RowsViewModel { }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG005");
    }

    [Fact]
    public void Invalid_template_configuration_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row
            {
                [DataGridColumn(Kind = DataGridColumnKind.Template)]
                public string Value { get; set; } = "";
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG009");
    }

    [Fact]
    public void Schema_emits_canonical_manifest_and_typed_item_key()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(SchemaId = "orders/v2")]
            public sealed class Row
            {
                [DataGridKey]
                public int Id { get; init; }

                [DataGridColumn(ColumnKey = "display-name")]
                public string Name { get; set; } = "";
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("IDataGridGeneratedSchemaManifestProvider", result.CombinedSource);
        Assert.Contains("IDataGridItemKey<global::Demo.Row, int>", result.CombinedSource);
        Assert.Contains("public const int ManifestVersion = 1", result.CombinedSource);
        Assert.Contains("public const string SchemaId = \"orders/v2\"", result.CombinedSource);
        Assert.Contains("DataGridGeneratedComparableField<global::Demo.Row, int> Id", result.CombinedSource);
        Assert.Contains("DataGridGeneratedStringField<global::Demo.Row, string> Name", result.CombinedSource);
        Assert.Contains("(0, \"Id\", \"Id\"", result.CombinedSource);
        Assert.Contains("(1, \"display-name\", \"Name\"", result.CombinedSource);
        Assert.Contains("public int GetKey(global::Demo.Row item)", result.CombinedSource);
        Assert.Contains("=> item.Id;", result.CombinedSource);
        Assert.Contains("CreateItemIndex()", result.CombinedSource);
        Assert.Contains("IEqualityComparer<int> KeyComparer", result.CombinedSource);
        Assert.Contains("CreateIdentitySelectionModel()", result.CombinedSource);
        Assert.Contains("DataGridStateOptions CreateStateOptions", result.CombinedSource);
        Assert.Contains("ItemKeySelector = static item => ((global::Demo.Row)item).Id", result.CombinedSource);
        Assert.Contains("Array.AsReadOnly(s_fields)", result.CombinedSource);
        Assert.DoesNotContain("CreateStreamBuffer", result.CombinedSource);
    }

    [Fact]
    public void Field_can_define_typed_item_key()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridKey]
                public readonly long Id;

                public string Name { get; set; } = "";
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("IDataGridItemKey<global::Demo.Row, long>", result.CombinedSource);
        Assert.Contains("public long GetKey(global::Demo.Row item)", result.CombinedSource);
    }

    [Fact]
    public void Streaming_keyed_schema_generates_bounded_buffer_factory()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(Streaming = true)]
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                public string Name { get; set; } = "";
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("DataGridGeneratedStreamBuffer<global::Demo.Row, int> CreateStreamBuffer", result.CombinedSource);
        Assert.Contains("DataGridGeneratedAsyncStreamPump<global::Demo.Row, int> CreateAsyncStreamPump", result.CombinedSource);
        Assert.Contains("DataGridGeneratedStreamOverflowPolicy.CoalesceByKey", result.CombinedSource);
    }

    [Fact]
    public void Duplicate_item_keys_report_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                [DataGridKey] public int AlternateId { get; init; }
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG101");
        Assert.DoesNotContain("IDataGridItemKey<", result.CombinedSource);
    }

    [Fact]
    public void Duplicate_column_keys_report_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridColumn(ColumnKey = "value")] public int First { get; set; }
                [DataGridColumn(ColumnKey = "value")] public int Second { get; set; }
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG100");
    }

    [Fact]
    public void Nullable_item_key_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridKey] public string? Id { get; init; }
                public string Name { get; set; } = "";
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG101");
    }

    [Fact]
    public void Generated_output_is_deterministic()
    {
        const string source = """
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row { public int Id { get; set; } public string Name { get; set; } = ""; }
            """;

        GeneratorTestResult first = GeneratorTestHelper.Run(source);
        GeneratorTestResult second = GeneratorTestHelper.Run(source);
        Assert.Equal(first.CombinedSource, second.CombinedSource);
    }

    [Fact]
    public void Unchanged_schema_output_is_reused_when_another_schema_changes()
    {
        const string firstSource = """
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class First { public int Id { get; set; } }
            [GenerateDataGridColumns]
            public sealed class Second { public string Name { get; set; } = ""; }
            """;
        const string secondSource = """
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class First { public int Id { get; set; } public decimal Amount { get; set; } }
            [GenerateDataGridColumns]
            public sealed class Second { public string Name { get; set; } = ""; }
            """;

        IncrementalRunResult result = GeneratorTestHelper.RunIncremental(
            firstSource,
            secondSource,
            "DirectSchemaSources");

        Assert.Contains(IncrementalStepRunReason.Modified, result.Reasons);
        Assert.Contains(IncrementalStepRunReason.Unchanged, result.Reasons);
        Assert.Equal(3, result.Sources.Count); // injected attributes plus two schemas
    }

    [Fact]
    public void Unchanged_direct_schema_semantic_candidate_is_reused()
    {
        const string firstBefore = """
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class First { public int Id { get; set; } }
            """;
        const string firstAfter = """
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class First { public int Id { get; set; } public decimal Amount { get; set; } }
            """;
        const string unchangedSecond = """
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Second { public string Name { get; set; } = ""; }
            """;

        IncrementalRunResult result = GeneratorTestHelper.RunIncremental(
            new[] { firstBefore, unchangedSecond },
            new[] { firstAfter, unchangedSecond },
            "DirectSchemaCandidates");

        Assert.Contains(IncrementalStepRunReason.Modified, result.Reasons);
        Assert.Contains(IncrementalStepRunReason.Unchanged, result.Reasons);
    }

    [Fact]
    public void Indexed_column_family_generates_typed_method_backed_factories()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;

            [GenerateDataGridIndexedColumns(
                Name = "Cells",
                GetterMethod = nameof(GetCell),
                SetterMethod = nameof(SetCell),
                NotificationNameMethod = nameof(GetCellName))]
            public sealed class SheetRow
            {
                public object? GetCell(int index) => null;
                public void SetCell(int index, object? value) { }
                public static string GetCellName(int index) => "Cell" + index;
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Contains("public static class SheetRowCells", result.CombinedSource);
        Assert.Contains("CreateColumn<TValue>", result.CombinedSource);
        Assert.Contains("item => (TValue)item.GetCell(index)!", result.CombinedSource);
        Assert.Contains("SheetRow.GetCellName(index)", result.CombinedSource);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Canonical_manifest_contains_export_editor_remote_and_accessibility_metadata()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            using Avalonia.Controls;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridColumn(
                    ColumnKey = "amount",
                    Header = "Amount fallback",
                    Description = "Description fallback",
                    HeaderProviderMethod = nameof(GetAmountHeader),
                    DescriptionProviderMethod = nameof(GetAmountDescription),
                    ExportFormat = "N2",
                    ExportNullText = "-",
                    BackendFieldName = "total_amount",
                    FilterEditor = DataGridGeneratedFilterEditorKind.Range,
                    FilterEditorResourceKey = "AmountEditor",
                    HeaderResourceKey = "AmountHeader",
                    DescriptionResourceKey = "AmountDescription",
                    AutomationId = "amount-cell",
                    AutomationName = "Amount",
                    AutomationHelpText = "Order amount",
                    IsSensitive = true)]
                public decimal Amount { get; set; }

                public static string GetAmountHeader(System.IFormatProvider provider) => "Amount";
                public static string GetAmountDescription() => "Order amount";
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Contains("exportFormat: \"N2\"", result.CombinedSource);
        Assert.Contains("backendFieldName: \"total_amount\"", result.CombinedSource);
        Assert.Contains("filterEditor: (global::Avalonia.Controls.DataGridGeneratedFilterEditorKind)6", result.CombinedSource);
        Assert.Contains("automationId: \"amount-cell\"", result.CombinedSource);
        Assert.Contains("isSensitive: true", result.CombinedSource);
        Assert.Contains("headerProvider: static provider => global::Demo.Row.GetAmountHeader(provider)", result.CombinedSource);
        Assert.Contains("descriptionProvider: static provider => global::Demo.Row.GetAmountDescription()", result.CombinedSource);
        Assert.Contains("global::Demo.Row.GetAmountHeader(global::System.Globalization.CultureInfo.CurrentUICulture)", result.CombinedSource);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Template_factory_methods_generate_typed_recycling_templates()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            using Avalonia.Controls;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridColumn(
                    DataGridColumnKind.Template,
                    TemplateFactoryMethod = nameof(BuildCell),
                    EditingTemplateFactoryMethod = nameof(BuildEditor),
                    ReuseCellContent = true)]
                public string Name { get; set; } = "";

                public static Control BuildCell(Row item, Control existing) => existing ?? new TextBlock();
                public static Control BuildEditor(Row item, Control existing) => existing ?? new TextBox();
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Contains("column.CellTemplate = new global::Avalonia.Controls.DataGridGeneratedFuncDataTemplate", result.CombinedSource);
        Assert.Contains("column.CellEditingTemplate = new global::Avalonia.Controls.DataGridGeneratedFuncDataTemplate", result.CombinedSource);
        Assert.Contains("column.ReuseCellContent = true", result.CombinedSource);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Analytics_attributes_generate_typed_pivot_chart_outline_and_formula_roles()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            using Avalonia.Controls;
            using Avalonia.Controls.DataGridPivoting;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridPivotAxis(DataGridGeneratedAnalyticsRole.PivotRow, Order = 0, Name = "Desk")]
                [DataGridChartField(DataGridGeneratedAnalyticsRole.ChartCategory, Order = 0)]
                [DataGridOutlineField(DataGridGeneratedAnalyticsRole.OutlineGroup, Order = 0)]
                public string Desk { get; set; } = "";

                [DataGridPivotValue(PivotAggregateType.Sum, Format = "N2", DisplayMode = PivotValueDisplayMode.PercentOfGrandTotal)]
                [DataGridChartField(DataGridGeneratedAnalyticsRole.ChartValue, Series = "Amount")]
                [DataGridFormulaField("Amount", Dependencies = new[] { "Desk" })]
                public decimal Amount { get; set; }
            }
            """);

        Assert.Empty(result.GeneratorDiagnostics);
        Assert.Contains("IDataGridGeneratedAnalyticsField[] s_analyticsFields", result.CombinedSource);
        Assert.Contains("DataGridGeneratedAnalyticsRole)1", result.CombinedSource);
        Assert.Contains("DataGridGeneratedAnalyticsRole)8", result.CombinedSource);
        Assert.Contains("DataGridGeneratedAnalyticsRole)2048", result.CombinedSource);
        Assert.Contains("CreatePivotAxisFields", result.CombinedSource);
        Assert.Contains("CreatePivotValueFields", result.CombinedSource);
        Assert.Contains("DataGridGeneratedDiagnosticsManifest Diagnostics", result.CombinedSource);
        Assert.Contains("DataGridGeneratedAnalyticsRole)2120", result.CombinedSource);
        Assert.Contains("CreateColumnLayoutController", result.CombinedSource);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Invalid_formula_dependency_and_duplicate_name_report_diagnostics()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridFormulaField("Value", Dependencies = new[] { "missing" })]
                public decimal Amount { get; set; }

                [DataGridFormulaField("Value")]
                public decimal Total { get; set; }
            }
            """);

        Assert.Equal(2, result.GeneratorDiagnostics.Count(diagnostic => diagnostic.Id == "PDGSG121"));
    }

    [Fact]
    public void Invalid_indexed_column_method_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridIndexedColumns(GetterMethod = "Missing")]
            public sealed class SheetRow { }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG004");
    }

    [Fact]
    public void Named_controller_generates_grouped_lifetime_api_and_schema()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            using Avalonia.Controls;
            namespace Demo;
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                public string Name { get; init; } = "";
            }

            [GenerateDataGridController(
                typeof(Row),
                "Trades",
                Features = DataGridGeneratedFeatures.Columns | DataGridGeneratedFeatures.Sorting | DataGridGeneratedFeatures.Searching,
                OperationExecution = DataGridOperationExecution.ExternalPipeline)]
            public sealed partial class TradingViewModel
            {
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("class RowDataGridSchema", result.CombinedSource);
        Assert.Contains("DataGridGeneratedOperationController<global::Demo.Row> Trades", result.CombinedSource);
        Assert.Contains("InitializeTrades", result.CombinedSource);
        Assert.Contains("CreateTradesController", result.CombinedSource);
        Assert.Contains("DisposeTrades", result.CombinedSource);
        Assert.Contains("(global::Avalonia.Controls.DataGridOperationExecution)1", result.CombinedSource);
        Assert.Contains("(global::Avalonia.Controls.DataGridGeneratedFeatures)11", result.CombinedSource);
    }

    [Fact]
    public void Multiple_named_controllers_are_supported_but_duplicate_names_report_diagnostic()
    {
        GeneratorTestResult valid = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridController(typeof(Row), "Primary")]
            [GenerateDataGridController(typeof(Row), "Secondary")]
            public sealed partial class DashboardViewModel { }
            """);
        AssertNoErrors(valid);
        Assert.Contains("InitializePrimary", valid.CombinedSource);
        Assert.Contains("InitializeSecondary", valid.CombinedSource);

        GeneratorTestResult duplicate = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridController(typeof(Row), "Grid")]
            [GenerateDataGridController(typeof(Row), "Grid")]
            public sealed partial class DashboardViewModel { }
            """);
        Assert.Contains(duplicate.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG117");
    }

    [Fact]
    public void Missing_controller_source_member_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridController(typeof(Row), "Rows", SourceMember = "Missing")]
            public sealed partial class RowsViewModel { }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG103");
    }

    [Fact]
    public void Incompatible_controller_source_type_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridController(typeof(Row), "Rows", SourceMember = nameof(Source))]
            public sealed partial class RowsViewModel
            {
                public int Source { get; } = 42;
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG103");
    }

    [Fact]
    public void Stream_source_requires_external_operation_execution()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridController(
                typeof(Row),
                "Rows",
                SourceMember = nameof(Source),
                SourceKind = DataGridGeneratedSourceKind.AsyncEnumerable,
                OperationExecution = DataGridOperationExecution.View)]
            public sealed partial class RowsViewModel
            {
                public IAsyncEnumerable<Row> Source { get; } = null!;
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG104");
    }

    [Fact]
    public void Hierarchy_attributes_generate_typed_options_and_parent_key_accessor()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Node
            {
                [DataGridKey] public int Id { get; init; }
                [DataGridParentKey] public int? ParentId { get; init; }
                [DataGridChildren] public List<Node> Children { get; } = new();
                [DataGridExpanded] public bool IsExpanded { get; set; }
                public string Name { get; set; } = "";
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("HierarchicalOptions<global::Demo.Node> CreateHierarchicalOptions", result.CombinedSource);
        Assert.Contains("ChildrenSelector = static item => item.Children", result.CombinedSource);
        Assert.Contains("IsExpandedSetter = static (item, value) => item.IsExpanded = value", result.CombinedSource);
        Assert.Contains("ExpandedStateKeySelector = static item => item.Id", result.CombinedSource);
        Assert.Contains("CreateHierarchyController()", result.CombinedSource);
        Assert.Contains("CreateSelectionController(", result.CombinedSource);
        Assert.Contains("int? GetParentKey", result.CombinedSource);
    }

    [Fact]
    public void Hierarchy_child_loader_is_validated_and_emitted_for_options_and_controller()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using System.Threading;
            using System.Threading.Tasks;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Node
            {
                [DataGridKey] public int Id { get; init; }
                [DataGridChildren(LoaderMethod = nameof(LoadChildrenAsync))]
                public List<Node> Children { get; } = new();
                public ValueTask<IReadOnlyList<Node>> LoadChildrenAsync(CancellationToken cancellationToken) =>
                    ValueTask.FromResult<IReadOnlyList<Node>>(Children);
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("ChildrenSelectorAsync = static async (item, cancellationToken) => await item.LoadChildrenAsync", result.CombinedSource);
        Assert.Contains("static (item, cancellationToken) => item.LoadChildrenAsync(cancellationToken)", result.CombinedSource);
    }

    [Fact]
    public void Invalid_hierarchy_child_loader_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Node
            {
                [DataGridKey] public int Id { get; init; }
                [DataGridChildren(LoaderMethod = nameof(LoadChildren))]
                public List<Node> Children { get; } = new();
                public List<Node> LoadChildren() => Children;
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG109");
    }

    [Fact]
    public void Invalid_hierarchy_member_reports_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Node
            {
                [DataGridChildren] public string Children { get; set; } = "";
                [DataGridExpanded] public bool IsExpanded { get; init; }
                public string Name { get; set; } = "";
            }
            """);

        Assert.Equal(2, result.GeneratorDiagnostics.Count(diagnostic => diagnostic.Id == "PDGSG109"));
    }

    [Fact]
    public void State_version_and_column_aliases_emit_versioned_state_controller()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(SchemaId = "trades", StateVersion = 3)]
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                [DataGridColumn(ColumnKey = "amount", PreviousColumnKeys = new[] { "price", "value" })]
                public decimal Amount { get; init; }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("public const int StateVersion = 3", result.CombinedSource);
        Assert.Contains("[\"price\"] = \"amount\"", result.CombinedSource);
        Assert.Contains("[\"value\"] = \"amount\"", result.CombinedSource);
        Assert.Contains("CreateStateController(", result.CombinedSource);
    }

    [Fact]
    public void Duplicate_column_alias_reports_state_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                [DataGridColumn(PreviousColumnKeys = new[] { "legacy" })] public string First { get; init; } = "";
                [DataGridColumn(PreviousColumnKeys = new[] { "legacy" })] public string Second { get; init; } = "";
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG118");
    }

    [Fact]
    public void Controller_key_member_generates_typed_identity_without_item_attribute()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row
            {
                public int Id { get; init; }
                public string Name { get; init; } = "";
            }

            [GenerateDataGridController(typeof(Row), "Rows", KeyMember = nameof(Row.Id))]
            public sealed partial class RowsViewModel { }
            """);

        AssertNoErrors(result);
        Assert.Contains("IDataGridItemKey<global::Demo.Row, int>", result.CombinedSource);
        Assert.Contains("CreateItemIndex", result.CombinedSource);
    }

    [Fact]
    public void Direct_schema_controller_key_must_use_data_grid_key_attribute()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row { public int Id { get; init; } }
            [GenerateDataGridController(typeof(Row), "Rows", KeyMember = nameof(Row.Id))]
            public sealed partial class RowsViewModel { }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG101");
    }

    [Fact]
    public void Controller_supports_validated_factory_and_configure_hook()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }

            public sealed class Factory : IDataGridGeneratedControllerFactory<Row>
            {
                public DataGridGeneratedOperationController<Row> Create(
                    in DataGridGeneratedControllerContext<Row> context) =>
                    new(context.Schema, context.Options.Execution, context.Options.Features);
            }

            [GenerateDataGridController(
                typeof(Row),
                "Rows",
                ImplementationType = typeof(Factory),
                ConfigureMethod = nameof(ConfigureRows))]
            public sealed partial class RowsViewModel
            {
                private static void ConfigureRows(ref DataGridGeneratedControllerOptions<Row> options)
                {
                    options.Features = DataGridGeneratedFeatures.Columns;
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("ConfigureRows(ref options)", result.CombinedSource);
        Assert.Contains("return new global::Demo.Factory().Create(in context)", result.CombinedSource);
    }

    [Fact]
    public void Source_cache_controller_emits_owned_dynamic_data_pipeline()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo
            {
                public sealed class Row
                {
                    [DataGridKey] public int Id { get; init; }
                    public string Name { get; init; } = "";
                }

                [GenerateDataGridController(
                    typeof(Row),
                    "Rows",
                    SourceMember = nameof(Source),
                    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceCache,
                    OperationExecution = DataGridOperationExecution.ExternalPipeline)]
                public sealed partial class RowsViewModel
                {
                    private readonly global::DynamicData.SourceCache<Row, int> Source = new(static row => row.Id);
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("ConnectRowsPipeline", result.CombinedSource);
        Assert.Contains("SortAndBind", result.CombinedSource);
        Assert.Contains("RowsErrors", result.CombinedSource);
        Assert.Contains("DisconnectRowsPipeline", result.CombinedSource);
    }

    [Fact]
    public void Source_cache_controller_requires_matching_stable_key()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo
            {
                public sealed class Row { public int Id { get; init; } }
                [GenerateDataGridController(
                    typeof(Row),
                    "Rows",
                    SourceMember = nameof(Source),
                    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceCache,
                    OperationExecution = DataGridOperationExecution.ExternalPipeline)]
                public sealed partial class RowsViewModel
                {
                    private readonly global::DynamicData.SourceCache<Row, int> Source = new(static row => row.Id);
                }
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG101");
    }

    [Fact]
    public void Source_list_controller_emits_compilable_owned_pipeline()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo
            {
                public sealed class Row { public int Id { get; init; } }
                [GenerateDataGridController(
                    typeof(Row),
                    "Rows",
                    SourceMember = nameof(Source),
                    SourceKind = DataGridGeneratedSourceKind.DynamicDataSourceList,
                    OperationExecution = DataGridOperationExecution.ExternalPipeline)]
                public sealed partial class RowsViewModel
                {
                    private readonly global::DynamicData.SourceList<Row> Source = new();
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("changes.ObserveOn(scheduler)", result.CombinedSource);
        Assert.Contains("ReadOnlyObservableCollection<global::Demo.Row> items", result.CombinedSource);
    }

    [Fact]
    public void Async_enumerable_controller_emits_bounded_stream_lifecycle()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                public string Name { get; init; } = "";
            }

            [GenerateDataGridController(
                typeof(Row),
                "Rows",
                SourceMember = nameof(Source),
                SourceKind = DataGridGeneratedSourceKind.AsyncEnumerable,
                OperationExecution = DataGridOperationExecution.ExternalPipeline)]
            public sealed partial class RowsViewModel
            {
                private readonly IAsyncEnumerable<Row> Source = null!;
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("RunRowsStreamAsync", result.CombinedSource);
        Assert.Contains("CreateAsyncStreamPump", result.CombinedSource);
        Assert.Contains("RowsStreamMetrics", result.CombinedSource);
        Assert.Contains("StopRowsStream", result.CombinedSource);
    }

    [Fact]
    public void Remote_controller_emits_query_lifecycle_and_validates_provider_key()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Threading;
            using System.Threading.Tasks;
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                public string Name { get; init; } = "";
            }

            public sealed class Provider : IDataGridQueryProvider<Row, int>
            {
                public ValueTask<DataGridQueryPage<Row, int>> ExecuteAsync(
                    DataGridRemoteQuery<Row> query,
                    CancellationToken cancellationToken) =>
                    ValueTask.FromResult(new DataGridQueryPage<Row, int>(query.Revision, new Row[0]));
            }

            [GenerateDataGridController(
                typeof(Row),
                "Rows",
                SourceMember = nameof(Source),
                SourceKind = DataGridGeneratedSourceKind.Remote,
                OperationExecution = DataGridOperationExecution.Remote)]
            public sealed partial class RowsViewModel
            {
                private readonly Provider Source = new();
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("CreateRowsRemoteQueryController", result.CombinedSource);
        Assert.Contains("QueryRowsAsync", result.CombinedSource);
        Assert.Contains("DataGridRemoteQuery<global::Demo.Row>", result.CombinedSource);
        Assert.Contains("DisposeRowsRemoteQuery", result.CombinedSource);
    }

    [Fact]
    public void Editable_columns_generate_typed_fields_hooks_and_keyed_controller()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridKey] public int Id { get; init; }
                [DataGridColumn(
                    FormatString = "0.00",
                    ParserMethod = nameof(ParseAmount),
                    FormatterMethod = nameof(FormatAmount),
                    ValidatorMethod = nameof(ValidateAmount),
                    AsyncValidatorMethod = nameof(ValidateAmountAsync),
                    CoerceMethod = nameof(CoerceAmount),
                    CanEditMethod = nameof(CanEditAmount))]
                public decimal Amount { get; set; }

                public static bool ParseAmount(ReadOnlySpan<char> text, IFormatProvider provider, out decimal value) =>
                    decimal.TryParse(text, provider, out value);
                public static string FormatAmount(decimal value, IFormatProvider provider) => value.ToString("0.00", provider);
                public static string? ValidateAmount(Row item, decimal value) => value < 0 ? "negative" : null;
                public static ValueTask<string?> ValidateAmountAsync(Row item, decimal value, CancellationToken cancellationToken) =>
                    ValueTask.FromResult<string?>(null);
                public static decimal CoerceAmount(Row item, decimal value) => decimal.Round(value, 2);
                public static bool CanEditAmount(Row item) => true;
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("DataGridGeneratedEditField<global::Demo.Row, decimal>", result.CombinedSource);
        Assert.Contains("global::Demo.Row.ParseAmount", result.CombinedSource);
        Assert.Contains("global::Demo.Row.ValidateAmountAsync", result.CombinedSource);
        Assert.Contains("IReadOnlyList<global::Avalonia.Controls.IDataGridGeneratedEditField<global::Demo.Row>> EditFields", result.CombinedSource);
        Assert.Contains("CreateEditController", result.CombinedSource);
        Assert.Contains("CreateClipboardController", result.CombinedSource);
        Assert.Contains("CreateFillController", result.CombinedSource);
        Assert.Contains("CreateDragDropController", result.CombinedSource);
    }

    [Fact]
    public void Incompatible_edit_hook_reports_customization_diagnostic()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row
            {
                [DataGridColumn(ParserMethod = nameof(ParseAmount))]
                public decimal Amount { get; set; }
                public static decimal ParseAmount(string text) => 0m;
            }
            """);

        Assert.Contains(result.GeneratorDiagnostics, diagnostic => diagnostic.Id == "PDGSG004");
    }

    [Fact]
    public void Common_data_annotations_compile_into_direct_edit_validation()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.ComponentModel.DataAnnotations;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [Required, StringLength(12, MinimumLength = 3)]
                public string Name { get; set; } = "";
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("String.IsNullOrWhiteSpace(value)", result.CombinedSource);
        Assert.Contains("value.Length > 12", result.CombinedSource);
        Assert.Contains("value.Length < 3", result.CombinedSource);
    }

    [Fact]
    public void Group_summary_conditional_format_and_band_metadata_share_typed_accessors()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns]
            public sealed class Row
            {
                [DataGridGroup(Order = 1)]
                [DataGridSummary(DataGridAggregateType.Sum, Scope = DataGridSummaryScope.Both)]
                [DataGridConditionalFormat(DataGridCondition.GreaterThan, Operand = "100", CellThemeKey = "LargeValue")]
                [DataGridBand("Trading/Risk", Order = 2)]
                public decimal Value { get; set; }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("DataGridGeneratedGroupField<global::Demo.Row, decimal>", result.CombinedSource);
        Assert.Contains("DataGridGeneratedSummary<global::Demo.Row, decimal>", result.CombinedSource);
        Assert.Contains("Comparer<decimal>.Default.Compare(value, (decimal)100m) > 0", result.CombinedSource);
        Assert.Contains("DataGridGeneratedBandField(\"Value\", new string[] { \"Trading\", \"Risk\" }, 2)", result.CombinedSource);
    }

    [Fact]
    public void Performance_profile_emits_explicit_options_and_streaming_search_policy()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            [GenerateDataGridColumns(PerformanceProfile = DataGridGeneratedPerformanceProfile.HighFrequencyStreaming)]
            public sealed class Row { public int Id { get; set; } }
            """);

        AssertNoErrors(result);
        Assert.Contains("CreatePerformanceOptions", result.CombinedSource);
        Assert.Contains("DataGridGeneratedPerformanceProfile)6", result.CombinedSource);
        Assert.Contains("HighPerformanceSearchTrackItemChanges = false", result.CombinedSource);
    }

    [Fact]
    public void Generated_view_recipe_emits_customizable_toolbar_and_stable_metadata()
    {
        GeneratorTestResult result = GeneratorTestHelper.Run("""
            using System.Collections.Generic;
            using Avalonia.Controls;
            using ProDataGrid.SourceGeneration;
            namespace Demo;
            public sealed class Row { public int Id { get; set; } }
            [GenerateDataGridView(
                typeof(Row),
                Recipe = DataGridViewRecipe.Explorer,
                ControllerName = "Rows",
                AutomationId = "rows-grid")]
            public sealed class RowsViewModel
            {
                public IReadOnlyList<Row> Items { get; } = new Row[0];
                public DataGridColumnDefinitionList ColumnDefinitions { get; } = new();
                public DataGridFastPathOptions FastPathOptions { get; } = new();
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("GeneratedRecipe = 3", result.CombinedSource);
        Assert.Contains("GeneratedAutomationId = \"rows-grid\"", result.CombinedSource);
        Assert.Contains("GeneratedControllerName = \"Rows\"", result.CombinedSource);
        Assert.Contains("CreateGeneratedToolbar", result.CombinedSource);
        Assert.Contains("GeneratedToolbarSlot", result.CombinedSource);
        Assert.Contains("CreateGeneratedRecipeContent", result.CombinedSource);
        Assert.Contains("GeneratedExplorerSlot", result.CombinedSource);
    }

    private static void AssertNoErrors(GeneratorTestResult result)
    {
        Assert.True(
            !result.Errors.Any(),
            string.Join(Environment.NewLine, result.Errors.Select(static diagnostic => diagnostic.ToString())) +
            Environment.NewLine + result.CombinedSource);
    }
}
