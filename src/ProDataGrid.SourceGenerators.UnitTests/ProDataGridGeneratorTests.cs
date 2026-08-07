// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using System.Linq;
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

    private static void AssertNoErrors(GeneratorTestResult result)
    {
        Assert.True(
            !result.Errors.Any(),
            string.Join(Environment.NewLine, result.Errors.Select(static diagnostic => diagnostic.ToString())) +
            Environment.NewLine + result.CombinedSource);
    }
}
