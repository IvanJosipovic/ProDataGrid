// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

#nullable disable

using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace Avalonia.Controls.DataGridEditing
{
    /// <summary>Describes a reflection-free generated editing activation profile.</summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    readonly struct DataGridGeneratedEditingInteractionProfile : IEquatable<DataGridGeneratedEditingInteractionProfile>
    {
        /// <summary>Initializes an editing activation profile.</summary>
        public DataGridGeneratedEditingInteractionProfile(
            DataGridEditTriggers editTriggers,
            bool restrictTextInputToCells = true,
            KeyModifiers requiredPointerModifiers = KeyModifiers.None,
            bool requireExactPointerModifiers = false)
        {
            EditTriggers = editTriggers;
            RestrictTextInputToCells = restrictTextInputToCells;
            RequiredPointerModifiers = requiredPointerModifiers;
            RequireExactPointerModifiers = requireExactPointerModifiers;
        }

        /// <summary>Gets the declared edit triggers.</summary>
        public DataGridEditTriggers EditTriggers { get; }

        /// <summary>Gets whether text input must originate from a cell in the owning grid.</summary>
        public bool RestrictTextInputToCells { get; }

        /// <summary>Gets the pointer modifiers required to activate editing.</summary>
        public KeyModifiers RequiredPointerModifiers { get; }

        /// <summary>Gets whether the pointer modifiers must exactly match the required set.</summary>
        public bool RequireExactPointerModifiers { get; }

        /// <inheritdoc />
        public bool Equals(DataGridGeneratedEditingInteractionProfile other) =>
            EditTriggers == other.EditTriggers &&
            RestrictTextInputToCells == other.RestrictTextInputToCells &&
            RequiredPointerModifiers == other.RequiredPointerModifiers &&
            RequireExactPointerModifiers == other.RequireExactPointerModifiers;

        /// <inheritdoc />
        public override bool Equals(object obj) =>
            obj is DataGridGeneratedEditingInteractionProfile other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(
            (int)EditTriggers,
            RestrictTextInputToCells,
            (int)RequiredPointerModifiers,
            RequireExactPointerModifiers);

        /// <summary>Tests two profiles for equality.</summary>
        public static bool operator ==(
            DataGridGeneratedEditingInteractionProfile left,
            DataGridGeneratedEditingInteractionProfile right) => left.Equals(right);

        /// <summary>Tests two profiles for inequality.</summary>
        public static bool operator !=(
            DataGridGeneratedEditingInteractionProfile left,
            DataGridGeneratedEditingInteractionProfile right) => !left.Equals(right);
    }

    /// <summary>Applies a generated editing activation profile through the DataGrid interaction boundary.</summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedEditingInteractionModel : DataGridEditingInteractionModel
    {
        /// <summary>Initializes a generated editing interaction model.</summary>
        public DataGridGeneratedEditingInteractionModel(DataGridGeneratedEditingInteractionProfile profile)
        {
            Profile = profile;
        }

        /// <summary>Gets the immutable activation profile.</summary>
        public DataGridGeneratedEditingInteractionProfile Profile { get; }

        /// <inheritdoc />
        public override bool IsTextInputFromGrid(DataGridTextInputContext context) =>
            base.IsTextInputFromGrid(new DataGridTextInputContext(
                context.Grid,
                context.Source,
                Profile.RestrictTextInputToCells));

        /// <inheritdoc />
        public override bool ShouldBeginEditOnPointer(DataGridPointerEditContext context)
        {
            KeyModifiers required = Profile.RequiredPointerModifiers;
            bool modifiersMatch = Profile.RequireExactPointerModifiers
                ? context.Modifiers == required
                : (context.Modifiers & required) == required;
            return modifiersMatch && base.ShouldBeginEditOnPointer(new DataGridPointerEditContext(
                context.Grid,
                context.IsDoubleClick,
                Profile.EditTriggers,
                context.Modifiers));
        }

        /// <inheritdoc />
        public override string GetTextInputForEdit(DataGridTextInputEditContext context) =>
            base.GetTextInputForEdit(new DataGridTextInputEditContext(
                context.Grid,
                context.Text,
                context.IsEditing,
                context.IsReadOnly,
                context.CanEditCurrentCell,
                Profile.EditTriggers,
                context.Modifiers));
    }

    /// <summary>Creates generated editing interaction models from one immutable profile.</summary>
#if !DATAGRID_INTERNAL
    public
#else
    internal
#endif
    sealed class DataGridGeneratedEditingInteractionModelFactory : IDataGridEditingInteractionModelFactory
    {
        /// <summary>Initializes a generated editing interaction-model factory.</summary>
        public DataGridGeneratedEditingInteractionModelFactory(DataGridGeneratedEditingInteractionProfile profile)
        {
            Profile = profile;
        }

        /// <summary>Gets the immutable activation profile.</summary>
        public DataGridGeneratedEditingInteractionProfile Profile { get; }

        /// <inheritdoc />
        public IDataGridEditingInteractionModel Create() => new DataGridGeneratedEditingInteractionModel(Profile);
    }
}
