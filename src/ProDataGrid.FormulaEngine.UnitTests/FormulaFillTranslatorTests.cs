// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using ProDataGrid.FormulaEngine.Excel;
using Xunit;

namespace ProDataGrid.FormulaEngine.UnitTests
{
    public sealed class FormulaFillTranslatorTests
    {
        [Fact]
        public void A1_copy_shifts_only_relative_reference_dimensions()
        {
            var translator = new ExcelFormulaFillTranslator();

            bool translated = translator.TryTranslate(
                "=A1+$B2+C$3+$D$4+SUM(E5:F6)+Table1[Amount]",
                sourceRow: 4,
                sourceColumn: 2,
                targetRow: 6,
                targetColumn: 5,
                out string result);

            Assert.True(translated);
            Assert.Equal("=D3+$B4+F$3+$D$4+SUM(H7:I8)+Table1[Amount]", result);
        }

        [Fact]
        public void R1C1_copy_preserves_relative_offsets_and_absolute_coordinates()
        {
            var translator = new ExcelFormulaFillTranslator(FormulaReferenceMode.R1C1);

            bool translated = translator.TryTranslate(
                "=R[-1]C[2]+R1C1",
                sourceRow: 5,
                sourceColumn: 5,
                targetRow: 9,
                targetColumn: 8,
                out string result);

            Assert.True(translated);
            Assert.Equal("=R[-1]C[2]+R1C1", result);
        }

        [Fact]
        public void Copy_outside_A1_bounds_emits_reference_error()
        {
            var translator = new ExcelFormulaFillTranslator();

            bool translated = translator.TryTranslate(
                "=A1",
                sourceRow: 1,
                sourceColumn: 1,
                targetRow: 0,
                targetColumn: 0,
                out string result);

            Assert.True(translated);
            Assert.Equal("=#REF!", result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("42")]
        [InlineData("=not valid +")]
        public void Non_formula_or_invalid_text_is_not_translated(string input)
        {
            var translator = new ExcelFormulaFillTranslator();

            Assert.False(translator.TryTranslate(input, 0, 0, 1, 1, out string result));
            Assert.Equal(input, result);
        }

        [Fact]
        public void Parsed_expression_translation_covers_arrays_unary_nodes_and_overflow()
        {
            var relative = new FormulaReferenceAddress(
                FormulaReferenceMode.A1,
                row: 2,
                column: 3,
                rowIsAbsolute: false,
                columnIsAbsolute: false);
            var overflow = new FormulaReferenceAddress(
                FormulaReferenceMode.A1,
                row: int.MaxValue,
                column: 1,
                rowIsAbsolute: false,
                columnIsAbsolute: true);
            var array = new FormulaArrayExpression(new FormulaExpression[,]
            {
                {
                    new FormulaUnaryExpression(
                        FormulaUnaryOperator.Negate,
                        new FormulaReferenceExpression(new FormulaReference(relative))),
                    new FormulaNameExpression("TaxRate")
                }
            });

            FormulaArrayExpression translated = Assert.IsType<FormulaArrayExpression>(
                FormulaReferenceTranslator.TranslateForCopy(array, rowOffset: 4, columnOffset: 5));
            FormulaUnaryExpression unary = Assert.IsType<FormulaUnaryExpression>(translated[0, 0]);
            FormulaReferenceAddress address = Assert.IsType<FormulaReferenceExpression>(unary.Operand).Reference.Start;
            Assert.Equal((6, 8), (address.Row, address.Column));
            Assert.Same(array[0, 1], translated[0, 1]);

            FormulaExpression invalid = FormulaReferenceTranslator.TranslateForCopy(
                new FormulaReferenceExpression(new FormulaReference(overflow)),
                rowOffset: 1,
                columnOffset: 0);
            Assert.Equal(FormulaErrorType.Ref, Assert.IsType<FormulaLiteralExpression>(invalid).Value.AsError().Type);
        }

        [Fact]
        public void Zero_offset_reuses_expression_and_null_is_rejected()
        {
            var expression = new FormulaNameExpression("Revenue");

            Assert.Same(expression, FormulaReferenceTranslator.TranslateForCopy(expression, 0, 0));
            Assert.Throws<System.ArgumentNullException>(() =>
                FormulaReferenceTranslator.TranslateForCopy(null!, 1, 1));
        }
    }
}
