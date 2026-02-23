// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Avalonia.Controls
{
    /// <summary>
    /// Default DataGrid state serializer based on built-in System.Text.Json.
    /// </summary>
    #if !DATAGRID_INTERNAL
    public
    #else
    internal
    #endif
    sealed class SystemTextJsonDataGridStateSerializer : IDataGridStateSerializer
    {
        public string FormatId => "json/system-text";

        public byte[] Serialize(DataGridPersistedState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return JsonSerializer.SerializeToUtf8Bytes(
                state,
                DataGridPersistedStateJsonContext.Default.DataGridPersistedState);
        }

        public DataGridPersistedState Deserialize(ReadOnlySpan<byte> payload)
        {
            var state = JsonSerializer.Deserialize(
                payload,
                DataGridPersistedStateJsonContext.Default.DataGridPersistedState);

            if (state == null)
            {
                throw new DataGridStatePersistenceException("Deserialized state payload is null.");
            }

            return state;
        }

        public string SerializeToString(DataGridPersistedState state)
        {
            return Encoding.UTF8.GetString(Serialize(state));
        }

        public DataGridPersistedState Deserialize(string payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            return Deserialize(Encoding.UTF8.GetBytes(payload));
        }
    }

    [JsonSourceGenerationOptions(
        GenerationMode = JsonSourceGenerationMode.Default,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        WriteIndented = false)]
    [JsonSerializable(typeof(DataGridPersistedState))]
    [JsonSerializable(typeof(DataGridPersistedState.CellState))]
    [JsonSerializable(typeof(DataGridPersistedState.ColumnLayoutState))]
    [JsonSerializable(typeof(DataGridPersistedState.ColumnState))]
    [JsonSerializable(typeof(DataGridPersistedState.ConditionalFormattingDescriptorState))]
    [JsonSerializable(typeof(DataGridPersistedState.ConditionalFormattingState))]
    [JsonSerializable(typeof(DataGridPersistedState.DataGridLengthValue))]
    [JsonSerializable(typeof(DataGridPersistedState.FilteringDescriptorState))]
    [JsonSerializable(typeof(DataGridPersistedState.FilteringState))]
    [JsonSerializable(typeof(DataGridPersistedState.GroupDescriptionState))]
    [JsonSerializable(typeof(DataGridPersistedState.GroupState))]
    [JsonSerializable(typeof(DataGridPersistedState.GroupingState))]
    [JsonSerializable(typeof(DataGridPersistedState.HierarchicalState))]
    [JsonSerializable(typeof(DataGridPersistedState.PersistedValue))]
    [JsonSerializable(typeof(DataGridPersistedState.ScrollSample))]
    [JsonSerializable(typeof(DataGridPersistedState.ScrollState))]
    [JsonSerializable(typeof(DataGridPersistedState.SearchCurrentState))]
    [JsonSerializable(typeof(DataGridPersistedState.SearchDescriptorState))]
    [JsonSerializable(typeof(DataGridPersistedState.SearchState))]
    [JsonSerializable(typeof(DataGridPersistedState.SelectionState))]
    [JsonSerializable(typeof(DataGridPersistedState.SortingDescriptorState))]
    [JsonSerializable(typeof(DataGridPersistedState.SortingState))]
    internal sealed partial class DataGridPersistedStateJsonContext : JsonSerializerContext
    {
    }
}
