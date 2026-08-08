// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using ProDataGrid.SourceGeneration;
using ProDataGrid.SourceGeneration.AotSmoke;

[assembly: GenerateDataGridRegistry(
    RegistryName = "AotGeneratedRegistry",
    RegistryNamespace = "ProDataGrid.SourceGeneration.AotSmoke.Generated")]
[assembly: DataGridViewRegistration(typeof(AotTradeViewModel), typeof(AotRegisteredView))]
