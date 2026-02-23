// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using System.Globalization;

namespace Avalonia.Controls
{
    internal static class DataGridStatePersistenceValueConverter
    {
        private const string NullType = "null";
        private const string StringType = "string";
        private const string BoolType = "bool";
        private const string ByteType = "byte";
        private const string SByteType = "sbyte";
        private const string Int16Type = "int16";
        private const string UInt16Type = "uint16";
        private const string Int32Type = "int32";
        private const string UInt32Type = "uint32";
        private const string Int64Type = "int64";
        private const string UInt64Type = "uint64";
        private const string SingleType = "single";
        private const string DoubleType = "double";
        private const string DecimalType = "decimal";
        private const string GuidType = "guid";
        private const string DateTimeType = "datetime";
        private const string DateTimeOffsetType = "datetimeoffset";
        private const string TimeSpanType = "timespan";
        private const string CharType = "char";

        public static bool TryWriteValue(object value, out DataGridPersistedState.PersistedValue persisted, out string reason)
        {
            reason = null;

            switch (value)
            {
                case null:
                    persisted = new DataGridPersistedState.PersistedValue { Type = NullType };
                    return true;
                case string text:
                    persisted = new DataGridPersistedState.PersistedValue { Type = StringType, Value = text };
                    return true;
                case bool boolean:
                    persisted = new DataGridPersistedState.PersistedValue
                    {
                        Type = BoolType,
                        Value = boolean ? bool.TrueString : bool.FalseString
                    };
                    return true;
                case byte number:
                    persisted = Number(ByteType, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case sbyte number:
                    persisted = Number(SByteType, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case short number:
                    persisted = Number(Int16Type, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case ushort number:
                    persisted = Number(UInt16Type, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case int number:
                    persisted = Number(Int32Type, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case uint number:
                    persisted = Number(UInt32Type, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case long number:
                    persisted = Number(Int64Type, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case ulong number:
                    persisted = Number(UInt64Type, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case float number:
                    persisted = Number(SingleType, number.ToString("R", CultureInfo.InvariantCulture));
                    return true;
                case double number:
                    persisted = Number(DoubleType, number.ToString("R", CultureInfo.InvariantCulture));
                    return true;
                case decimal number:
                    persisted = Number(DecimalType, number.ToString(CultureInfo.InvariantCulture));
                    return true;
                case Guid guid:
                    persisted = new DataGridPersistedState.PersistedValue { Type = GuidType, Value = guid.ToString("D", CultureInfo.InvariantCulture) };
                    return true;
                case DateTime dateTime:
                    persisted = new DataGridPersistedState.PersistedValue { Type = DateTimeType, Value = dateTime.ToString("O", CultureInfo.InvariantCulture) };
                    return true;
                case DateTimeOffset dateTimeOffset:
                    persisted = new DataGridPersistedState.PersistedValue { Type = DateTimeOffsetType, Value = dateTimeOffset.ToString("O", CultureInfo.InvariantCulture) };
                    return true;
                case TimeSpan timeSpan:
                    persisted = new DataGridPersistedState.PersistedValue { Type = TimeSpanType, Value = timeSpan.ToString("c", CultureInfo.InvariantCulture) };
                    return true;
                case char @char:
                    persisted = new DataGridPersistedState.PersistedValue { Type = CharType, Value = @char.ToString() };
                    return true;
                default:
                    persisted = null;
                    reason = $"Unsupported persisted value type '{value.GetType().FullName}'.";
                    return false;
            }
        }

        public static bool TryReadValue(DataGridPersistedState.PersistedValue persisted, out object value, out string reason)
        {
            value = null;
            reason = null;

            if (persisted == null || string.IsNullOrEmpty(persisted.Type))
            {
                reason = "Persisted value is null or missing type.";
                return false;
            }

            switch (persisted.Type)
            {
                case NullType:
                    value = null;
                    return true;
                case StringType:
                    value = persisted.Value ?? string.Empty;
                    return true;
                case BoolType:
                    if (bool.TryParse(persisted.Value, out var parsedBool))
                    {
                        value = parsedBool;
                        return true;
                    }
                    reason = $"Cannot parse boolean value '{persisted.Value}'.";
                    return false;
                case ByteType:
                    if (byte.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedByte))
                    {
                        value = parsedByte;
                        return true;
                    }
                    reason = $"Cannot parse byte value '{persisted.Value}'.";
                    return false;
                case SByteType:
                    if (sbyte.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSByte))
                    {
                        value = parsedSByte;
                        return true;
                    }
                    reason = $"Cannot parse sbyte value '{persisted.Value}'.";
                    return false;
                case Int16Type:
                    if (short.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt16))
                    {
                        value = parsedInt16;
                        return true;
                    }
                    reason = $"Cannot parse int16 value '{persisted.Value}'.";
                    return false;
                case UInt16Type:
                    if (ushort.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUInt16))
                    {
                        value = parsedUInt16;
                        return true;
                    }
                    reason = $"Cannot parse uint16 value '{persisted.Value}'.";
                    return false;
                case Int32Type:
                    if (int.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt32))
                    {
                        value = parsedInt32;
                        return true;
                    }
                    reason = $"Cannot parse int32 value '{persisted.Value}'.";
                    return false;
                case UInt32Type:
                    if (uint.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUInt32))
                    {
                        value = parsedUInt32;
                        return true;
                    }
                    reason = $"Cannot parse uint32 value '{persisted.Value}'.";
                    return false;
                case Int64Type:
                    if (long.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt64))
                    {
                        value = parsedInt64;
                        return true;
                    }
                    reason = $"Cannot parse int64 value '{persisted.Value}'.";
                    return false;
                case UInt64Type:
                    if (ulong.TryParse(persisted.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedUInt64))
                    {
                        value = parsedUInt64;
                        return true;
                    }
                    reason = $"Cannot parse uint64 value '{persisted.Value}'.";
                    return false;
                case SingleType:
                    if (float.TryParse(persisted.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedSingle))
                    {
                        value = parsedSingle;
                        return true;
                    }
                    reason = $"Cannot parse single value '{persisted.Value}'.";
                    return false;
                case DoubleType:
                    if (double.TryParse(persisted.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedDouble))
                    {
                        value = parsedDouble;
                        return true;
                    }
                    reason = $"Cannot parse double value '{persisted.Value}'.";
                    return false;
                case DecimalType:
                    if (decimal.TryParse(persisted.Value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedDecimal))
                    {
                        value = parsedDecimal;
                        return true;
                    }
                    reason = $"Cannot parse decimal value '{persisted.Value}'.";
                    return false;
                case GuidType:
                    if (Guid.TryParse(persisted.Value, out var parsedGuid))
                    {
                        value = parsedGuid;
                        return true;
                    }
                    reason = $"Cannot parse guid value '{persisted.Value}'.";
                    return false;
                case DateTimeType:
                    if (DateTime.TryParseExact(persisted.Value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDateTime))
                    {
                        value = parsedDateTime;
                        return true;
                    }
                    reason = $"Cannot parse datetime value '{persisted.Value}'.";
                    return false;
                case DateTimeOffsetType:
                    if (DateTimeOffset.TryParseExact(persisted.Value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedDateTimeOffset))
                    {
                        value = parsedDateTimeOffset;
                        return true;
                    }
                    reason = $"Cannot parse datetimeoffset value '{persisted.Value}'.";
                    return false;
                case TimeSpanType:
                    if (TimeSpan.TryParseExact(persisted.Value, "c", CultureInfo.InvariantCulture, out var parsedTimeSpan))
                    {
                        value = parsedTimeSpan;
                        return true;
                    }
                    reason = $"Cannot parse timespan value '{persisted.Value}'.";
                    return false;
                case CharType:
                    if (!string.IsNullOrEmpty(persisted.Value) && persisted.Value.Length == 1)
                    {
                        value = persisted.Value[0];
                        return true;
                    }
                    reason = $"Cannot parse char value '{persisted.Value}'.";
                    return false;
                default:
                    reason = $"Unknown persisted value type '{persisted.Type}'.";
                    return false;
            }
        }

        private static DataGridPersistedState.PersistedValue Number(string type, string value)
        {
            return new DataGridPersistedState.PersistedValue
            {
                Type = type,
                Value = value
            };
        }
    }
}
