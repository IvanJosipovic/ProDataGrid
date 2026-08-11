// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;

namespace ProDataGrid.FormulaEngine
{
    public enum FormulaReferenceKind
    {
        Cell,
        Range
    }

    public readonly struct FormulaSheetReference : IEquatable<FormulaSheetReference>
    {
        public FormulaSheetReference(string? workbookName, string? startSheetName, string? endSheetName = null)
        {
            WorkbookName = workbookName;
            StartSheetName = startSheetName;
            EndSheetName = endSheetName;
        }

        public string? WorkbookName { get; }

        public string? StartSheetName { get; }

        public string? EndSheetName { get; }

        public bool IsExternal => !string.IsNullOrWhiteSpace(WorkbookName);

        public bool IsRange =>
            !string.IsNullOrWhiteSpace(EndSheetName) &&
            !string.Equals(StartSheetName, EndSheetName, StringComparison.OrdinalIgnoreCase);

        public FormulaSheetReference WithWorkbook(string? workbookName)
        {
            return new FormulaSheetReference(workbookName, StartSheetName, EndSheetName);
        }

        public FormulaSheetReference WithSheets(string? startSheetName, string? endSheetName = null)
        {
            return new FormulaSheetReference(WorkbookName, startSheetName, endSheetName);
        }

        public bool Equals(FormulaSheetReference other)
        {
            return string.Equals(WorkbookName, other.WorkbookName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(StartSheetName, other.StartSheetName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(EndSheetName, other.EndSheetName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is FormulaSheetReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + (WorkbookName == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(WorkbookName));
                hash = (hash * 31) + (StartSheetName == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(StartSheetName));
                hash = (hash * 31) + (EndSheetName == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(EndSheetName));
                return hash;
            }
        }

        public override string ToString()
        {
            var sheetPart = StartSheetName ?? string.Empty;
            if (IsRange)
            {
                sheetPart = $"{StartSheetName}:{EndSheetName}";
            }

            return string.IsNullOrWhiteSpace(WorkbookName)
                ? sheetPart
                : $"[{WorkbookName}]{sheetPart}";
        }

        public static bool operator ==(FormulaSheetReference left, FormulaSheetReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FormulaSheetReference left, FormulaSheetReference right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct FormulaReferenceAddress : IEquatable<FormulaReferenceAddress>
    {
        public FormulaReferenceAddress(
            FormulaReferenceMode mode,
            int row,
            int column,
            bool rowIsAbsolute,
            bool columnIsAbsolute,
            FormulaSheetReference? sheet = null)
        {
            Mode = mode;
            Row = row;
            Column = column;
            RowIsAbsolute = rowIsAbsolute;
            ColumnIsAbsolute = columnIsAbsolute;
            Sheet = sheet;
        }

        public FormulaReferenceMode Mode { get; }

        public int Row { get; }

        public int Column { get; }

        public bool RowIsAbsolute { get; }

        public bool ColumnIsAbsolute { get; }

        public FormulaSheetReference? Sheet { get; }

        public FormulaReferenceAddress WithSheet(FormulaSheetReference? sheet)
        {
            return new FormulaReferenceAddress(Mode, Row, Column, RowIsAbsolute, ColumnIsAbsolute, sheet);
        }

        public bool Equals(FormulaReferenceAddress other)
        {
            return Mode == other.Mode &&
                   Row == other.Row &&
                   Column == other.Column &&
                   RowIsAbsolute == other.RowIsAbsolute &&
                   ColumnIsAbsolute == other.ColumnIsAbsolute &&
                   Nullable.Equals(Sheet, other.Sheet);
        }

        public override bool Equals(object? obj)
        {
            return obj is FormulaReferenceAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + Mode.GetHashCode();
                hash = (hash * 31) + Row.GetHashCode();
                hash = (hash * 31) + Column.GetHashCode();
                hash = (hash * 31) + RowIsAbsolute.GetHashCode();
                hash = (hash * 31) + ColumnIsAbsolute.GetHashCode();
                hash = (hash * 31) + (Sheet?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public override string ToString()
        {
            return Sheet == null
                ? $"{Mode}:{Row},{Column}"
                : $"{Sheet}!{Mode}:{Row},{Column}";
        }

        public static bool operator ==(FormulaReferenceAddress left, FormulaReferenceAddress right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FormulaReferenceAddress left, FormulaReferenceAddress right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct FormulaReference : IEquatable<FormulaReference>
    {
        public FormulaReference(FormulaReferenceAddress cell)
        {
            Kind = FormulaReferenceKind.Cell;
            Start = cell;
            End = cell;
        }

        public FormulaReference(FormulaReferenceAddress start, FormulaReferenceAddress end)
        {
            Kind = FormulaReferenceKind.Range;
            Start = start;
            End = end;
        }

        public FormulaReferenceKind Kind { get; }

        public FormulaReferenceAddress Start { get; }

        public FormulaReferenceAddress End { get; }

        public bool IsSingleCell => Kind == FormulaReferenceKind.Cell;

        public bool Equals(FormulaReference other)
        {
            return Kind == other.Kind && Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override bool Equals(object? obj)
        {
            return obj is FormulaReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 31) + Kind.GetHashCode();
                hash = (hash * 31) + Start.GetHashCode();
                hash = (hash * 31) + End.GetHashCode();
                return hash;
            }
        }

        public override string ToString()
        {
            return Kind == FormulaReferenceKind.Cell
                ? Start.ToString()
                : $"{Start}:{End}";
        }

        public static bool operator ==(FormulaReference left, FormulaReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(FormulaReference left, FormulaReference right)
        {
            return !left.Equals(right);
        }
    }
}
