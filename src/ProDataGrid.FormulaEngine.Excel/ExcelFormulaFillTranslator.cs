// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Globalization;
using ProDataGrid.FormulaEngine;

namespace ProDataGrid.FormulaEngine.Excel
{
    /// <summary>Translates Excel A1 or R1C1 formula text for copy/fill operations.</summary>
    public sealed class ExcelFormulaFillTranslator : IPreparedFormulaFillTranslator
    {
        private readonly ExcelFormulaParser _parser = new();
        private readonly ExcelFormulaFormatter _formatter = new();
        private readonly FormulaParseOptions _parseOptions;
        private readonly FormulaFormatOptions _formatOptions;

        /// <summary>Initializes an A1 translator with invariant Excel separators.</summary>
        public ExcelFormulaFillTranslator()
            : this(FormulaReferenceMode.A1, ',', '.')
        {
        }

        /// <summary>Initializes a translator with the requested reference mode and separators.</summary>
        public ExcelFormulaFillTranslator(
            FormulaReferenceMode referenceMode,
            char argumentSeparator = ',',
            char decimalSeparator = '.')
        {
            _parseOptions = new FormulaParseOptions
            {
                ReferenceMode = referenceMode,
                ArgumentSeparator = argumentSeparator,
                DecimalSeparator = decimalSeparator,
                AllowLeadingEquals = true
            };
            _formatOptions = new FormulaFormatOptions
            {
                ReferenceMode = referenceMode,
                ArgumentSeparator = argumentSeparator,
                DecimalSeparator = decimalSeparator,
                IncludeLeadingEquals = true,
                Culture = CultureInfo.InvariantCulture
            };
        }

        /// <inheritdoc />
        public bool TryTranslate(
            string formulaText,
            int sourceRow,
            int sourceColumn,
            int targetRow,
            int targetColumn,
            out string translatedFormula)
        {
            translatedFormula = formulaText ?? string.Empty;
            IPreparedFormulaFillTranslation? prepared = Prepare(formulaText, sourceRow, sourceColumn);
            return prepared != null && prepared.TryTranslate(targetRow, targetColumn, out translatedFormula);
        }

        /// <inheritdoc />
        public IPreparedFormulaFillTranslation? Prepare(string formulaText, int sourceRow, int sourceColumn)
        {
            if (string.IsNullOrWhiteSpace(formulaText) || formulaText[0] != '=' ||
                sourceRow < 0 || sourceColumn < 0)
            {
                return null;
            }

            try
            {
                FormulaExpression expression = _parser.Parse(formulaText, _parseOptions);
                return new PreparedTranslation(
                    expression,
                    sourceRow,
                    sourceColumn,
                    _formatter,
                    _formatOptions);
            }
            catch (FormulaParseException)
            {
                return null;
            }
        }

        private sealed class PreparedTranslation : IPreparedFormulaFillTranslation
        {
            private readonly FormulaExpression _expression;
            private readonly int _sourceRow;
            private readonly int _sourceColumn;
            private readonly ExcelFormulaFormatter _formatter;
            private readonly FormulaFormatOptions _formatOptions;

            public PreparedTranslation(
                FormulaExpression expression,
                int sourceRow,
                int sourceColumn,
                ExcelFormulaFormatter formatter,
                FormulaFormatOptions formatOptions)
            {
                _expression = expression;
                _sourceRow = sourceRow;
                _sourceColumn = sourceColumn;
                _formatter = formatter;
                _formatOptions = formatOptions;
            }

            public bool TryTranslate(int targetRow, int targetColumn, out string translatedFormula)
            {
                translatedFormula = string.Empty;
                if (targetRow < 0 || targetColumn < 0)
                {
                    return false;
                }

                try
                {
                    int rowOffset = checked(targetRow - _sourceRow);
                    int columnOffset = checked(targetColumn - _sourceColumn);
                    FormulaExpression translated = FormulaReferenceTranslator.TranslateForCopy(
                        _expression,
                        rowOffset,
                        columnOffset);
                    translatedFormula = _formatter.Format(translated, _formatOptions);
                    return true;
                }
                catch (OverflowException)
                {
                    return false;
                }
            }
        }
    }
}
