using System;

namespace ProDataGrid.ExcelSample.Models;

/// <summary>
/// Describes a sheet tab reorder request using source and target indices.
/// </summary>
public readonly struct SheetTabReorderRequest : IEquatable<SheetTabReorderRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SheetTabReorderRequest"/> struct.
    /// </summary>
    /// <param name="fromIndex">The original index of the sheet tab.</param>
    /// <param name="toIndex">The desired insertion index for the sheet tab.</param>
    public SheetTabReorderRequest(int fromIndex, int toIndex)
    {
        FromIndex = fromIndex;
        ToIndex = toIndex;
    }

    /// <summary>
    /// Gets the original index of the sheet tab.
    /// </summary>
    public int FromIndex { get; }

    /// <summary>
    /// Gets the desired insertion index for the sheet tab.
    /// </summary>
    public int ToIndex { get; }

    /// <inheritdoc />
    public bool Equals(SheetTabReorderRequest other)
    {
        return FromIndex == other.FromIndex && ToIndex == other.ToIndex;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is SheetTabReorderRequest other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            return (FromIndex * 397) ^ ToIndex;
        }
    }

    /// <summary>
    /// Determines whether two instances are equal.
    /// </summary>
    public static bool operator ==(SheetTabReorderRequest left, SheetTabReorderRequest right) => left.Equals(right);

    /// <summary>
    /// Determines whether two instances are not equal.
    /// </summary>
    public static bool operator !=(SheetTabReorderRequest left, SheetTabReorderRequest right) => !left.Equals(right);
}
