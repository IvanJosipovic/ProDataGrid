// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;
using System.Collections.Generic;
using ProDataGrid.FormulaEngine;

namespace ProDataGrid.FormulaEngine.Excel
{
    /// <summary>Describes a formula syntax error without requiring an expression tree.</summary>
    public readonly struct ExcelFormulaSyntaxError : IEquatable<ExcelFormulaSyntaxError>
    {
        /// <summary>Initializes a syntax error.</summary>
        public ExcelFormulaSyntaxError(string message, int position, int length)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Position = position;
            Length = length;
        }

        /// <summary>Gets the parser message.</summary>
        public string Message { get; }

        /// <summary>Gets the zero-based character position.</summary>
        public int Position { get; }

        /// <summary>Gets the affected character count.</summary>
        public int Length { get; }

        /// <inheritdoc />
        public bool Equals(ExcelFormulaSyntaxError other) =>
            Position == other.Position &&
            Length == other.Length &&
            string.Equals(Message, other.Message, StringComparison.Ordinal);

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is ExcelFormulaSyntaxError other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Position;
                hash = (hash * 31) + Length;
                hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(Message);
                return hash;
            }
        }

        /// <summary>Returns whether two syntax errors are equal.</summary>
        public static bool operator ==(ExcelFormulaSyntaxError left, ExcelFormulaSyntaxError right) => left.Equals(right);

        /// <summary>Returns whether two syntax errors differ.</summary>
        public static bool operator !=(ExcelFormulaSyntaxError left, ExcelFormulaSyntaxError right) => !left.Equals(right);
    }

    /// <summary>
    /// Validates Excel formula syntax using the production tokenizer without allocating an expression tree.
    /// This is suitable for analyzers, editors, and other latency-sensitive validation paths.
    /// </summary>
    public static class ExcelFormulaSyntaxValidator
    {
        /// <summary>Validates a formula with default invariant A1 options.</summary>
        public static bool TryValidate(string formulaText, out ExcelFormulaSyntaxError error) =>
            TryValidate(formulaText, new FormulaParseOptions(), out error);

        /// <summary>Validates a formula with explicit tokenizer options.</summary>
        public static bool TryValidate(
            string formulaText,
            FormulaParseOptions options,
            out ExcelFormulaSyntaxError error)
        {
            if (formulaText == null)
            {
                throw new ArgumentNullException(nameof(formulaText));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            try
            {
                IReadOnlyList<FormulaToken> tokens = new ExcelFormulaTokenizer(options).Tokenize(formulaText);
                new SyntaxParser(tokens).Validate();
                error = default;
                return true;
            }
            catch (FormulaParseException exception)
            {
                int position = Math.Max(0, Math.Min(exception.Position, formulaText.Length));
                error = new ExcelFormulaSyntaxError(
                    exception.Message,
                    position,
                    position < formulaText.Length ? 1 : 0);
                return false;
            }
        }

        private sealed class SyntaxParser
        {
            private readonly IReadOnlyList<FormulaToken> _tokens;
            private int _index;

            public SyntaxParser(IReadOnlyList<FormulaToken> tokens)
            {
                _tokens = tokens;
            }

            public void Validate()
            {
                ParseExpression();
                Expect(FormulaTokenType.End);
            }

            private ValueShape ParseExpression(int minimumPrecedence = 0, bool allowUnion = true)
            {
                ValueShape left = ParseUnary();
                while (true)
                {
                    if (Peek().Type == FormulaTokenType.Colon)
                    {
                        if (!left.CanBeReference)
                        {
                            throw Error("Range operator requires a reference on the left side.", Peek());
                        }

                        Next();
                        ValueShape right = ParseUnary();
                        if (!right.CanBeReference)
                        {
                            throw Error("Range operator requires a reference on the right side.", Peek());
                        }
                        left = ValueShape.Reference;
                        continue;
                    }

                    if (!TryGetBinaryPrecedence(Peek(), allowUnion, out int precedence, out bool rightAssociative) ||
                        precedence < minimumPrecedence)
                    {
                        break;
                    }

                    Next();
                    ParseExpression(rightAssociative ? precedence : precedence + 1, allowUnion);
                    left = ValueShape.Value;
                }
                return left;
            }

            private ValueShape ParseUnary()
            {
                FormulaToken token = Peek();
                if (token.Type == FormulaTokenType.Operator && (token.Text == "+" || token.Text == "-"))
                {
                    Next();
                    ParseUnary();
                    return ValueShape.Value;
                }

                ValueShape result = ParsePrimary();
                while (Peek().Type == FormulaTokenType.Operator && Peek().Text == "%")
                {
                    Next();
                    result = ValueShape.Value;
                }
                return result;
            }

            private ValueShape ParsePrimary()
            {
                FormulaToken token = Peek();
                switch (token.Type)
                {
                    case FormulaTokenType.Number:
                    case FormulaTokenType.Text:
                    case FormulaTokenType.Boolean:
                        Next();
                        return ValueShape.Value;
                    case FormulaTokenType.Error:
                        Next();
                        if (!IsKnownError(token.Text))
                        {
                            throw Error($"Unknown error token '{token.Text}'.", token);
                        }
                        return ValueShape.Value;
                    case FormulaTokenType.Name:
                        return ParseName();
                    case FormulaTokenType.OpenParen:
                        Next();
                        ValueShape inner = ParseExpression();
                        Expect(FormulaTokenType.CloseParen);
                        return inner;
                    case FormulaTokenType.OpenBrace:
                        ParseArray();
                        return ValueShape.Value;
                    default:
                        throw Error($"Unexpected token '{token.Text}'.", token);
                }
            }

            private ValueShape ParseName()
            {
                Expect(FormulaTokenType.Name);
                if (Peek().Type == FormulaTokenType.Colon &&
                    Peek(1).Type == FormulaTokenType.Name &&
                    Peek(2).Type == FormulaTokenType.Exclamation)
                {
                    Next();
                    Next();
                    Next();
                    Expect(FormulaTokenType.Name);
                    return ValueShape.Reference;
                }

                if (Match(FormulaTokenType.Exclamation))
                {
                    Expect(FormulaTokenType.Name);
                    return ValueShape.Reference;
                }

                if (Match(FormulaTokenType.OpenParen))
                {
                    ParseArguments();
                    return ValueShape.Value;
                }

                return ValueShape.Reference;
            }

            private void ParseArguments()
            {
                while (true)
                {
                    FormulaToken token = Peek();
                    if (token.Type == FormulaTokenType.CloseParen)
                    {
                        Next();
                        return;
                    }

                    if (token.Type == FormulaTokenType.Comma || token.Type == FormulaTokenType.Semicolon)
                    {
                        Next();
                        continue;
                    }

                    ParseExpression(0, allowUnion: false);
                    if (Match(FormulaTokenType.Comma) || Match(FormulaTokenType.Semicolon))
                    {
                        continue;
                    }

                    Expect(FormulaTokenType.CloseParen);
                    return;
                }
            }

            private void ParseArray()
            {
                FormulaToken open = Expect(FormulaTokenType.OpenBrace);
                if (Match(FormulaTokenType.CloseBrace))
                {
                    throw Error("Array literal cannot be empty.", open);
                }

                int expectedColumns = -1;
                int currentColumns = 0;
                while (true)
                {
                    ParseExpression(0, allowUnion: false);
                    currentColumns++;
                    if (Match(FormulaTokenType.Comma))
                    {
                        continue;
                    }

                    if (Match(FormulaTokenType.Semicolon))
                    {
                        if (expectedColumns < 0)
                        {
                            expectedColumns = currentColumns;
                        }
                        else if (currentColumns != expectedColumns)
                        {
                            throw Error("Array literal rows must have the same length.", open);
                        }
                        currentColumns = 0;
                        continue;
                    }

                    if (Match(FormulaTokenType.CloseBrace))
                    {
                        if (expectedColumns >= 0 && currentColumns != expectedColumns)
                        {
                            throw Error("Array literal rows must have the same length.", open);
                        }
                        return;
                    }

                    throw Error("Expected ',', ';', or '}' in array literal.", Peek());
                }
            }

            private FormulaToken Peek(int offset = 0)
            {
                int tokenIndex = Math.Min(_index + offset, _tokens.Count - 1);
                return _tokens[tokenIndex];
            }

            private FormulaToken Next() => _tokens[_index++];

            private FormulaToken Expect(FormulaTokenType type)
            {
                FormulaToken token = Next();
                if (token.Type != type)
                {
                    throw Error($"Expected {type} but found '{token.Text}'.", token);
                }
                return token;
            }

            private bool Match(FormulaTokenType type)
            {
                if (Peek().Type != type)
                {
                    return false;
                }
                _index++;
                return true;
            }

            private static bool TryGetBinaryPrecedence(
                FormulaToken token,
                bool allowUnion,
                out int precedence,
                out bool rightAssociative)
            {
                precedence = 0;
                rightAssociative = false;
                if (token.Type == FormulaTokenType.Intersection)
                {
                    precedence = 7;
                    return true;
                }
                if (allowUnion &&
                    (token.Type == FormulaTokenType.Comma || token.Type == FormulaTokenType.Semicolon))
                {
                    precedence = 6;
                    return true;
                }
                if (token.Type != FormulaTokenType.Operator)
                {
                    return false;
                }

                switch (token.Text)
                {
                    case "=":
                    case "<>":
                    case "<":
                    case "<=":
                    case ">":
                    case ">=":
                        precedence = 1;
                        return true;
                    case "&":
                        precedence = 2;
                        return true;
                    case "+":
                    case "-":
                        precedence = 3;
                        return true;
                    case "*":
                    case "/":
                        precedence = 4;
                        return true;
                    case "^":
                        precedence = 5;
                        rightAssociative = true;
                        return true;
                    default:
                        return false;
                }
            }

            private static bool IsKnownError(string token)
            {
                switch (token.ToUpperInvariant())
                {
                    case "#DIV/0!":
                    case "#N/A":
                    case "#NAME?":
                    case "#NULL!":
                    case "#NUM!":
                    case "#REF!":
                    case "#VALUE!":
                    case "#SPILL!":
                    case "#CALC!":
                        return true;
                    default:
                        return false;
                }
            }

            private static FormulaParseException Error(string message, FormulaToken token) =>
                new FormulaParseException(message, token.Start);

            private readonly struct ValueShape
            {
                private ValueShape(bool canBeReference) => CanBeReference = canBeReference;
                public bool CanBeReference { get; }
                public static ValueShape Value => new(false);
                public static ValueShape Reference => new(true);
            }
        }
    }
}
