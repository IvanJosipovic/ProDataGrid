using Avalonia.Controls;
using DataGridSample.Models.SourceGenerationAssembly;
using DataGridSample.Pages;
using DataGridSample.SourceGenerationPolicy.ViewModels;
using DataGridSample.ViewModels;
using ProDataGrid.SourceGeneration;

[assembly: GenerateDataGridRegistry(
    RegistryName = "SampleGeneratedSchemas",
    RegistryNamespace = "DataGridSample.Generated")]
[assembly: GenerateDataGridColumns(
    typeof(GeneratedAssemblyRow),
    ProviderName = "AssemblyGeneratedRowSchema",
    ProviderNamespace = "DataGridSample.Generated")]
[assembly: GenerateDataGridViewModel(
    typeof(GeneratedColumnsAssemblyViewModel),
    typeof(GeneratedAssemblyRow),
    ProviderName = "AssemblyGeneratedRowSchema")]
[assembly: GenerateDataGridColumnsForNamespace(
    "DataGridSample.Models.SourceGenerationPolicy",
    IncludeNestedNamespaces = false,
    Strict = true,
    Streaming = true,
    PerformanceProfile = DataGridGeneratedPerformanceProfile.HighFrequencyStreaming)]
[assembly: GenerateDataGridViewModelsForNamespace(
    "DataGridSample.SourceGenerationPolicy.ViewModels",
    IncludeNestedNamespaces = false,
    Strict = true,
    Streaming = true)]
[assembly: GenerateDataGridViewsForNamespace(
    "DataGridSample.SourceGenerationPolicy.ViewModels",
    IncludeNestedNamespaces = false,
    Framework = DataGridViewFramework.ReactiveUI,
    Recipe = DataGridViewRecipe.GridOnly,
    IsReadOnly = true,
    PerformanceProfile = DataGridGeneratedPerformanceProfile.HighFrequencyStreaming)]
[assembly: DataGridViewRegistration(
    typeof(GeneratedAssemblyNamespacePolicyPageViewModel),
    typeof(GeneratedAssemblyNamespacePolicyPage))]
