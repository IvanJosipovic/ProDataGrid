using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ProDataGrid.FormulaEngine;
using ProDataGrid.FormulaEngine.Excel;

namespace ProDataGrid.FormulaEngine.Benchmarks
{
    [MemoryDiagnoser]
    public sealed class FormulaEngineBenchmarks
    {
        private readonly ExcelFormulaParser _parser = new();
        private readonly ExcelFunctionRegistry _registry = new();
        private FormulaExpression _complexExpression = null!;
        private FormulaEvaluationContext _evaluationContext = null!;
        private DictionaryValueResolver _resolver = null!;
        private FormulaCalculationEngine _engine = null!;
        private BenchmarkWorkbook _workbook = null!;
        private BenchmarkWorksheet _worksheet = null!;
        private List<FormulaCellAddress> _formulaCells = null!;
        private FormulaCellAddress _dirtyCell;

        [GlobalSetup]
        public void Setup()
        {
            _complexExpression = _parser.Parse(
                "IF(SUM(A1:A1000)>1000,AVERAGE(B1:B1000),MAX(C1:C1000))",
                new FormulaParseOptions());

            _resolver = new DictionaryValueResolver();
            for (var row = 1; row <= 1000; row++)
            {
                _resolver.SetCell(new FormulaCellAddress("Sheet1", row, 1), FormulaValue.FromNumber(row));
                _resolver.SetCell(new FormulaCellAddress("Sheet1", row, 2), FormulaValue.FromNumber(row * 2));
                _resolver.SetCell(new FormulaCellAddress("Sheet1", row, 3), FormulaValue.FromNumber(row * 3));
            }

            var workbook = new BenchmarkWorkbook("Book1");
            var sheet = workbook.GetWorksheet("Sheet1");
            _evaluationContext = new FormulaEvaluationContext(workbook, sheet, new FormulaCellAddress("Sheet1", 1, 1), _registry);

            _engine = new FormulaCalculationEngine(_parser, _registry);
            _workbook = new BenchmarkWorkbook("Book1");
            _worksheet = (BenchmarkWorksheet)_workbook.GetWorksheet("Sheet1");
            _formulaCells = new List<FormulaCellAddress>();

            for (var row = 1; row <= 2000; row++)
            {
                var valueCell = _worksheet.GetCell(row, 1);
                valueCell.Value = FormulaValue.FromNumber(row);
                var formulaText = $"=A{row}*2";
                _engine.SetCellFormula(_worksheet, row, 2, formulaText);
                _formulaCells.Add(new FormulaCellAddress(_worksheet.Name, row, 2));
            }

            _dirtyCell = new FormulaCellAddress(_worksheet.Name, 1000, 1);
        }

        [Benchmark]
        public FormulaExpression Parse_ComplexFormula()
        {
            return _parser.Parse(
                "IF(SUM(A1:A1000)>1000,AVERAGE(B1:B1000),MAX(C1:C1000))",
                new FormulaParseOptions());
        }

        [Benchmark]
        public bool Validate_ComplexFormula()
        {
            return ExcelFormulaSyntaxValidator.TryValidate(
                "IF(SUM(A1:A1000)>1000,AVERAGE(B1:B1000),MAX(C1:C1000))",
                out _);
        }

        [Benchmark]
        public FormulaValue Evaluate_LargeRangeSum()
        {
            var evaluator = new FormulaEvaluator();
            return evaluator.Evaluate(_complexExpression, _evaluationContext, _resolver);
        }

        [Benchmark]
        public FormulaRecalculationResult Recalculate_FullGraph()
        {
            return _engine.Recalculate(_workbook, _formulaCells);
        }

        [Benchmark]
        public FormulaRecalculationResult Recalculate_Incremental()
        {
            var cell = _worksheet.GetCell(_dirtyCell.Row, _dirtyCell.Column);
            cell.Value = FormulaValue.FromNumber(cell.Value.AsNumber() + 1);
            return _engine.RecalculateIfAutomatic(_workbook, new[] { _dirtyCell });
        }

        private sealed class DictionaryValueResolver : IFormulaValueResolver
        {
            private readonly Dictionary<FormulaCellAddress, FormulaValue> _cells = new();

            public void SetCell(FormulaCellAddress address, FormulaValue value)
            {
                _cells[address] = value;
            }

            public bool TryResolveName(FormulaEvaluationContext context, string name, out FormulaValue value)
            {
                value = FormulaValue.FromError(new FormulaError(FormulaErrorType.Name));
                return false;
            }

            public bool TryResolveReference(FormulaEvaluationContext context, FormulaReference reference, out FormulaValue value)
            {
                var origin = new FormulaCellAddress(context.Worksheet.Name, context.Address.Row, context.Address.Column);
                if (reference.Kind == FormulaReferenceKind.Cell)
                {
                    if (!FormulaReferenceResolver.TryResolveCell(reference.Start, origin, out var address))
                    {
                        value = FormulaValue.FromError(new FormulaError(FormulaErrorType.Ref));
                        return true;
                    }

                    if (_cells.TryGetValue(address, out value))
                    {
                        return true;
                    }

                    value = FormulaValue.Blank;
                    return true;
                }

                if (!FormulaReferenceResolver.TryResolveRange(reference, origin, out var range))
                {
                    value = FormulaValue.FromError(new FormulaError(FormulaErrorType.Ref));
                    return true;
                }

                var rows = range.End.Row - range.Start.Row + 1;
                var columns = range.End.Column - range.Start.Column + 1;
                var array = new FormulaArray(rows, columns, range.Start);
                for (var row = 0; row < rows; row++)
                {
                    for (var column = 0; column < columns; column++)
                    {
                        var address = new FormulaCellAddress(range.Start.SheetName, range.Start.Row + row, range.Start.Column + column);
                        if (_cells.TryGetValue(address, out var cellValue))
                        {
                            array[row, column] = cellValue;
                        }
                        else
                        {
                            array[row, column] = FormulaValue.Blank;
                        }
                    }
                }

                value = FormulaValue.FromArray(array);
                return true;
            }
        }

        private sealed class BenchmarkWorkbook : IFormulaWorkbook
        {
            private readonly Dictionary<string, IFormulaWorksheet> _worksheets = new(StringComparer.OrdinalIgnoreCase);

            public BenchmarkWorkbook(string name)
            {
                Name = name;
                Settings = new FormulaCalculationSettings();
                AddWorksheet("Sheet1");
            }

            public string Name { get; }

            public FormulaCalculationSettings Settings { get; }

            public IReadOnlyList<IFormulaWorksheet> Worksheets => new List<IFormulaWorksheet>(_worksheets.Values);

            public IFormulaWorksheet GetWorksheet(string name)
            {
                if (!_worksheets.TryGetValue(name, out var sheet))
                {
                    throw new KeyNotFoundException($"Worksheet '{name}' not found.");
                }

                return sheet;
            }

            private void AddWorksheet(string name)
            {
                _worksheets[name] = new BenchmarkWorksheet(name, this);
            }
        }

        private sealed class BenchmarkWorksheet : IFormulaWorksheet
        {
            private readonly Dictionary<(int Row, int Column), BenchmarkCell> _cells = new();

            public BenchmarkWorksheet(string name, IFormulaWorkbook workbook)
            {
                Name = name;
                Workbook = workbook;
            }

            public string Name { get; }

            public IFormulaWorkbook Workbook { get; }

            public IFormulaCell GetCell(int row, int column)
            {
                if (!_cells.TryGetValue((row, column), out var cell))
                {
                    cell = new BenchmarkCell(new FormulaCellAddress(Name, row, column));
                    _cells[(row, column)] = cell;
                }

                return cell;
            }

            public bool TryGetCell(int row, int column, out IFormulaCell cell)
            {
                if (_cells.TryGetValue((row, column), out var existing))
                {
                    cell = existing;
                    return true;
                }

                cell = null!;
                return false;
            }
        }

        private sealed class BenchmarkCell : IFormulaCell
        {
            public BenchmarkCell(FormulaCellAddress address)
            {
                Address = address;
                Value = FormulaValue.Blank;
            }

            public FormulaCellAddress Address { get; }

            public string? Formula { get; set; }

            public FormulaExpression? Expression { get; set; }

            public FormulaValue Value { get; set; }
        }
    }
}
