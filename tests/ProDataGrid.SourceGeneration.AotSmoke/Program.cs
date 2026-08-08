// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using System;
using Avalonia.Controls;
using ProDataGrid.SourceGeneration.AotSmoke;
using ProDataGrid.SourceGeneration.AotSmoke.Generated;
using ProDataGrid.SourceGeneration.AotSmoke.Views;
using ReactiveUI.Avalonia;
using ReactiveUI.Builder;

RxAppBuilder.CreateReactiveUIBuilder()
    .WithAvalonia()
    .BuildApp();

using var viewModel = new AotTradeViewModel();
DataGridGeneratedCollectionMutationService<AotTradeRow> mutations =
    AotTradeRowSchema.CreateConfiguredCollectionMutationService(maximumItemsPerMutation: 8);
DataGridGeneratedNewRowService<AotTradeRow> newRows =
    AotTradeRowSchema.CreateConfiguredNewRowService();
using DataGridGeneratedEditController<AotTradeRow, int> edits = AotTradeRowSchema.CreateEditController();
DataGridGeneratedFillModel<AotTradeRow, int> formulaFill =
    AotTradeRowSchema.CreateConfiguredFormulaFillModel(edits);
AotTradeRow newRow = await newRows.CreateAsync();
await mutations.AddAsync(0, new[] { newRow });

if (!AotGeneratedRegistry.TryGetSchema(typeof(AotTradeRow), out IDataGridGeneratedSchemaManifestProvider schema) ||
    schema.Manifest.SchemaId != AotTradeRowSchema.SchemaId ||
    viewModel.ColumnDefinitions.Count != 5 ||
    !viewModel.FastPathOptions.StrictMode ||
    newRow.Id != 42)
{
    return 1;
}

if (!AotGeneratedRegistry.TryCreateView(viewModel, out Control? registeredView) ||
    registeredView is not AotRegisteredView)
{
    return 2;
}

Control avaloniaView = new AotGeneratedGridView(viewModel);
Control reactiveView = new AotGeneratedReactiveGridView(viewModel);
GC.KeepAlive(registeredView);
GC.KeepAlive(avaloniaView);
GC.KeepAlive(reactiveView);
GC.KeepAlive(formulaFill);
return 0;
