// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using Microsoft.CodeAnalysis;

namespace ProDataGrid.SourceGenerators;

/// <summary>
/// Generates reflection-free ProDataGrid schemas from attributes and assembly conventions.
/// </summary>
[Generator]
public sealed partial class ProDataGridGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        RegisterAttributeSources(context);

        IncrementalValueProvider<GenerationModel> model = context.CompilationProvider
            .Select(static (compilation, cancellationToken) => Discovery.Build(compilation, cancellationToken));

        context.RegisterSourceOutput(model, static (productionContext, generationModel) =>
        {
            foreach (Diagnostic diagnostic in generationModel.Diagnostics)
            {
                productionContext.ReportDiagnostic(diagnostic);
            }

            foreach (GeneratedSource source in Emitter.Emit(generationModel, productionContext.CancellationToken))
            {
                productionContext.AddSource(source.HintName, source.Source);
            }
        });
    }
}
