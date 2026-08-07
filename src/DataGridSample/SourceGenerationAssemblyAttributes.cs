using DataGridSample.Models.SourceGenerationAssembly;
using DataGridSample.ViewModels;
using ProDataGrid.SourceGeneration;

[assembly: GenerateDataGridColumns(
    typeof(GeneratedAssemblyRow),
    ProviderName = "AssemblyGeneratedRowSchema",
    ProviderNamespace = "DataGridSample.Generated")]
[assembly: GenerateDataGridViewModel(
    typeof(GeneratedColumnsAssemblyViewModel),
    typeof(GeneratedAssemblyRow),
    ProviderName = "AssemblyGeneratedRowSchema")]
