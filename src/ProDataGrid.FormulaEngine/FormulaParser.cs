// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

namespace ProDataGrid.FormulaEngine
{
    public interface IFormulaParser
    {
        FormulaExpression Parse(string formulaText, FormulaParseOptions options);
    }
}
