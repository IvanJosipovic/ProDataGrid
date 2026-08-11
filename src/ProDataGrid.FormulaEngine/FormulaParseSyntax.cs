// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable enable

using System;

namespace ProDataGrid.FormulaEngine
{
    /// <summary>Identifies the reference notation accepted by a formula parser.</summary>
    public enum FormulaReferenceMode
    {
        /// <summary>Uses column letters and one-based row numbers.</summary>
        A1,

        /// <summary>Uses explicit row and column coordinates.</summary>
        R1C1
    }

    /// <summary>Configures formula tokenization and parsing.</summary>
    public sealed class FormulaParseOptions
    {
        /// <summary>Gets or sets the accepted reference notation.</summary>
        public FormulaReferenceMode ReferenceMode { get; set; } = FormulaReferenceMode.A1;

        /// <summary>Gets or sets the function argument separator.</summary>
        public char ArgumentSeparator { get; set; } = ',';

        /// <summary>Gets or sets the numeric decimal separator.</summary>
        public char DecimalSeparator { get; set; } = '.';

        /// <summary>Gets or sets whether a leading equals sign is accepted.</summary>
        public bool AllowLeadingEquals { get; set; } = true;
    }

    /// <summary>Represents a formula syntax error at a character position.</summary>
    public sealed class FormulaParseException : Exception
    {
        /// <summary>Initializes a formula syntax error.</summary>
        public FormulaParseException(string message, int position)
            : base(message)
        {
            Position = position;
        }

        /// <summary>Gets the zero-based character position.</summary>
        public int Position { get; }
    }
}
