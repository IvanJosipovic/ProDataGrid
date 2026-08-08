// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using ProDataGrid.FormulaEngine.Excel;
using Xunit;

namespace ProDataGrid.FormulaEngine.Tests
{
    public sealed class ExcelFormulaParserTests
    {
        [Fact]
        public void Parse_A1_Reference()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("A1", new FormulaParseOptions());

            var reference = Assert.IsType<FormulaReferenceExpression>(expression);
            Assert.Equal(1, reference.Reference.Start.Row);
            Assert.Equal(1, reference.Reference.Start.Column);
            Assert.False(reference.Reference.Start.RowIsAbsolute);
            Assert.False(reference.Reference.Start.ColumnIsAbsolute);
        }

        [Fact]
        public void Parse_A1_Absolute_Reference()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("$C$10", new FormulaParseOptions());

            var reference = Assert.IsType<FormulaReferenceExpression>(expression);
            Assert.Equal(10, reference.Reference.Start.Row);
            Assert.Equal(3, reference.Reference.Start.Column);
            Assert.True(reference.Reference.Start.RowIsAbsolute);
            Assert.True(reference.Reference.Start.ColumnIsAbsolute);
        }

        [Fact]
        public void Parse_Sheet_Prefix_Reference()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("Sheet1!B2", new FormulaParseOptions());

            var reference = Assert.IsType<FormulaReferenceExpression>(expression);
            Assert.Equal("Sheet1", reference.Reference.Start.Sheet?.StartSheetName);
            Assert.Equal(2, reference.Reference.Start.Row);
            Assert.Equal(2, reference.Reference.Start.Column);
        }

        [Fact]
        public void Parse_R1C1_Relative_Reference()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("R[2]C[-1]", new FormulaParseOptions { ReferenceMode = FormulaReferenceMode.R1C1 });

            var reference = Assert.IsType<FormulaReferenceExpression>(expression);
            Assert.Equal(2, reference.Reference.Start.Row);
            Assert.Equal(-1, reference.Reference.Start.Column);
            Assert.False(reference.Reference.Start.RowIsAbsolute);
            Assert.False(reference.Reference.Start.ColumnIsAbsolute);
        }

        [Fact]
        public void Parse_Error_Literal()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("#N/A", new FormulaParseOptions());

            var literal = Assert.IsType<FormulaLiteralExpression>(expression);
            Assert.Equal(FormulaValueKind.Error, literal.Value.Kind);
            Assert.Equal(FormulaErrorType.NA, literal.Value.AsError().Type);
        }

        [Fact]
        public void Parse_Array_Literal()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("{1,2;3,4}", new FormulaParseOptions());

            var array = Assert.IsType<FormulaArrayExpression>(expression);
            Assert.Equal(2, array.RowCount);
            Assert.Equal(2, array.ColumnCount);

            var first = Assert.IsType<FormulaLiteralExpression>(array[0, 0]);
            Assert.Equal(1, first.Value.AsNumber());
        }

        [Fact]
        public void Parse_Percent_Operator()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("10%", new FormulaParseOptions());

            var unary = Assert.IsType<FormulaUnaryExpression>(expression);
            Assert.Equal(FormulaUnaryOperator.Percent, unary.Operator);
        }

        [Fact]
        public void Parse_Union_Operator()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("(A1,B1)", new FormulaParseOptions());

            var binary = Assert.IsType<FormulaBinaryExpression>(expression);
            Assert.Equal(FormulaBinaryOperator.Union, binary.Operator);
        }

        [Fact]
        public void Parse_Union_Operator_With_Semicolon_Separator()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("(A1;B1)", new FormulaParseOptions { ArgumentSeparator = ';' });

            var binary = Assert.IsType<FormulaBinaryExpression>(expression);
            Assert.Equal(FormulaBinaryOperator.Union, binary.Operator);
        }

        [Fact]
        public void Parse_Intersection_Operator()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("(A1 B1)", new FormulaParseOptions());

            var binary = Assert.IsType<FormulaBinaryExpression>(expression);
            Assert.Equal(FormulaBinaryOperator.Intersection, binary.Operator);
        }

        [Fact]
        public void Parse_Structured_Reference()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("Table1[Amount]", new FormulaParseOptions());

            var structured = Assert.IsType<FormulaStructuredReferenceExpression>(expression);
            Assert.Equal("Table1", structured.Reference.TableName);
            Assert.Equal("Amount", structured.Reference.ColumnStart);
            Assert.Equal(FormulaStructuredReferenceScope.None, structured.Reference.Scope);
        }

        [Fact]
        public void Parse_Structured_Reference_ThisRow()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("Table1[@Amount]", new FormulaParseOptions());

            var structured = Assert.IsType<FormulaStructuredReferenceExpression>(expression);
            Assert.Equal(FormulaStructuredReferenceScope.ThisRow, structured.Reference.Scope);
            Assert.Equal("Amount", structured.Reference.ColumnStart);
        }

        [Fact]
        public void Parse_Structured_Reference_Headers()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("Table1[#Headers]", new FormulaParseOptions());

            var structured = Assert.IsType<FormulaStructuredReferenceExpression>(expression);
            Assert.Equal(FormulaStructuredReferenceScope.Headers, structured.Reference.Scope);
            Assert.Null(structured.Reference.ColumnStart);
        }

        [Fact]
        public void Parse_Structured_Reference_Totals_Column()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("Table1[[#Totals],[Amount]]", new FormulaParseOptions());

            var structured = Assert.IsType<FormulaStructuredReferenceExpression>(expression);
            Assert.Equal(FormulaStructuredReferenceScope.Totals, structured.Reference.Scope);
            Assert.Equal("Amount", structured.Reference.ColumnStart);
        }

        [Fact]
        public void Parse_Sheet_Range_Reference()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("Sheet1:Sheet3!A1", new FormulaParseOptions());

            var reference = Assert.IsType<FormulaReferenceExpression>(expression);
            Assert.Equal("Sheet1", reference.Reference.Start.Sheet?.StartSheetName);
            Assert.Equal("Sheet3", reference.Reference.Start.Sheet?.EndSheetName);
        }

        [Fact]
        public void Parse_External_Reference()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("'[Book1]Sheet2'!B2", new FormulaParseOptions());

            var reference = Assert.IsType<FormulaReferenceExpression>(expression);
            Assert.Equal("Book1", reference.Reference.Start.Sheet?.WorkbookName);
            Assert.Equal("Sheet2", reference.Reference.Start.Sheet?.StartSheetName);
        }

        [Fact]
        public void Parse_Function_Allows_Empty_Arguments()
        {
            var parser = new ExcelFormulaParser();
            var expression = parser.Parse("IF(,1,)", new FormulaParseOptions());

            var call = Assert.IsType<FormulaFunctionCallExpression>(expression);
            Assert.Equal(3, call.Arguments.Count);

            var first = Assert.IsType<FormulaLiteralExpression>(call.Arguments[0]);
            var last = Assert.IsType<FormulaLiteralExpression>(call.Arguments[2]);
            Assert.Equal(FormulaValueKind.Blank, first.Value.Kind);
            Assert.Equal(FormulaValueKind.Blank, last.Value.Kind);
        }

        [Theory]
        [InlineData("=([@B]*[@C])*(1-[@D])")]
        [InlineData("=SUM(A1:A10,IF(B1>0,B1,0))")]
        [InlineData("=Table1[[#Totals],[Amount]]+Sheet1!A1")]
        [InlineData("={1,2;3,4}")]
        [InlineData("=Sheet1:Sheet3!A1")]
        public void Syntax_validator_accepts_runtime_formula_shapes(string formula)
        {
            Assert.True(ExcelFormulaSyntaxValidator.TryValidate(formula, out ExcelFormulaSyntaxError error), error.Message);
        }

        [Theory]
        [InlineData("=1+")]
        [InlineData("=SUM(1,")]
        [InlineData("={1,2;3}")]
        [InlineData("=#WHAT?")]
        public void Syntax_validator_reports_invalid_formula_shapes(string formula)
        {
            Assert.False(ExcelFormulaSyntaxValidator.TryValidate(formula, out ExcelFormulaSyntaxError error));
            Assert.NotEmpty(error.Message);
            Assert.InRange(error.Position, 0, formula.Length);
        }
    }
}
