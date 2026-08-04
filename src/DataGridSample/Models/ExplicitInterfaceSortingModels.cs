using System;

namespace DataGridSample.Models;

/// <summary>
/// Defines the direct explicit-interface sorting contract used by the sample.
/// </summary>
public interface IExplicitSortRow
{
    /// <summary>Gets the row name.</summary>
    string Name { get; }

    /// <summary>Gets the row timestamp.</summary>
    DateTime Time { get; }
}

/// <summary>
/// Defines a base interface whose property is implemented explicitly by sample rows.
/// </summary>
public interface IBaseExplicitSortRow
{
    /// <summary>Gets the inherited row name.</summary>
    string Name { get; }
}

/// <summary>
/// Extends the base sorting contract without redeclaring its property.
/// </summary>
public interface IInheritedExplicitSortRow : IBaseExplicitSortRow
{
    /// <summary>Gets the row priority.</summary>
    int Priority { get; }
}

/// <summary>
/// Defines nested details whose properties are implemented explicitly.
/// </summary>
public interface IExplicitSortDetail
{
    /// <summary>Gets the detail name.</summary>
    string Name { get; }

    /// <summary>Gets the detail score.</summary>
    int Score { get; }
}

/// <summary>
/// Defines a row with an explicitly implemented nullable nested object.
/// </summary>
public interface INestedExplicitSortRow
{
    /// <summary>Gets the stable row identifier.</summary>
    string Id { get; }

    /// <summary>Gets the optional nested details.</summary>
    IExplicitSortDetail? Detail { get; }
}

/// <summary>
/// Defines the primary label used to disambiguate same-name interface properties.
/// </summary>
public interface IPrimaryExplicitLabel
{
    /// <summary>Gets the primary label.</summary>
    string Label { get; }
}

/// <summary>
/// Defines the secondary label used to disambiguate same-name interface properties.
/// </summary>
public interface ISecondaryExplicitLabel
{
    /// <summary>Gets the secondary label.</summary>
    string Label { get; }
}

/// <summary>
/// Implements the direct row contract explicitly.
/// </summary>
public sealed class ExplicitSortRow : IExplicitSortRow
{
    private readonly string _name;
    private readonly DateTime _time;

    /// <summary>Initializes a direct explicit-interface row.</summary>
    public ExplicitSortRow(string name, DateTime time)
    {
        _name = name;
        _time = time;
    }

    string IExplicitSortRow.Name => _name;

    DateTime IExplicitSortRow.Time => _time;
}

/// <summary>
/// Provides a second runtime type for polymorphic explicit-interface sorting.
/// </summary>
public sealed class AlternateExplicitSortRow : IExplicitSortRow
{
    private readonly string _name;
    private readonly DateTime _time;

    /// <summary>Initializes a polymorphic explicit-interface row.</summary>
    public AlternateExplicitSortRow(string name, DateTime time)
    {
        _name = name;
        _time = time;
    }

    string IExplicitSortRow.Name => _name;

    DateTime IExplicitSortRow.Time => _time;
}

/// <summary>
/// Implements both an inherited and a directly declared interface property explicitly.
/// </summary>
public sealed class InheritedExplicitSortRow : IInheritedExplicitSortRow
{
    private readonly string _name;
    private readonly int _priority;

    /// <summary>Initializes an inherited explicit-interface row.</summary>
    public InheritedExplicitSortRow(string name, int priority)
    {
        _name = name;
        _priority = priority;
    }

    string IBaseExplicitSortRow.Name => _name;

    int IInheritedExplicitSortRow.Priority => _priority;
}

/// <summary>
/// Implements nested detail properties explicitly.
/// </summary>
public sealed class ExplicitSortDetail : IExplicitSortDetail
{
    private readonly string _name;
    private readonly int _score;

    /// <summary>Initializes nested explicit-interface details.</summary>
    public ExplicitSortDetail(string name, int score)
    {
        _name = name;
        _score = score;
    }

    string IExplicitSortDetail.Name => _name;

    int IExplicitSortDetail.Score => _score;
}

/// <summary>
/// Implements the root and nested-object properties explicitly.
/// </summary>
public sealed class NestedExplicitSortRow : INestedExplicitSortRow
{
    private readonly string _id;
    private readonly IExplicitSortDetail? _detail;

    /// <summary>Initializes a nested explicit-interface row.</summary>
    public NestedExplicitSortRow(string id, IExplicitSortDetail? detail)
    {
        _id = id;
        _detail = detail;
    }

    string INestedExplicitSortRow.Id => _id;

    IExplicitSortDetail? INestedExplicitSortRow.Detail => _detail;
}

/// <summary>
/// Implements two unrelated same-name interface properties explicitly.
/// </summary>
public sealed class DualExplicitLabelRow : IPrimaryExplicitLabel, ISecondaryExplicitLabel
{
    private readonly string _primaryLabel;
    private readonly string _secondaryLabel;

    /// <summary>Initializes a row with independently sortable interface labels.</summary>
    public DualExplicitLabelRow(string primaryLabel, string secondaryLabel)
    {
        _primaryLabel = primaryLabel;
        _secondaryLabel = secondaryLabel;
    }

    string IPrimaryExplicitLabel.Label => _primaryLabel;

    string ISecondaryExplicitLabel.Label => _secondaryLabel;
}
