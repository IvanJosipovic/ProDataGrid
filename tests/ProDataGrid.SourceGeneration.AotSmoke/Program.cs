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

if (!AotGeneratedRegistry.TryGetSchema(typeof(AotTradeRow), out IDataGridGeneratedSchemaManifestProvider schema) ||
    schema.Manifest.SchemaId != AotTradeRowSchema.SchemaId ||
    viewModel.ColumnDefinitions.Count != 5 ||
    !viewModel.FastPathOptions.StrictMode)
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
return 0;
