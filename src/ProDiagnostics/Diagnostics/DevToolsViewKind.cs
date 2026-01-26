namespace Avalonia.Diagnostics;

/// <summary>
/// Kinds of diagnostic views available in DevTools
/// </summary>
public enum DevToolsViewKind
{
    /// <summary>
    /// The Logical Tree diagnostic view
    /// </summary>
    LogicalTree = 0,
    /// <summary>
    /// The Visual Tree diagnostic view
    /// </summary>
    VisualTree = 1,
    /// <summary>
    /// Events diagnostic view
    /// </summary>
    Events = 2,
    /// <summary>
    /// The Combined Tree diagnostic view
    /// </summary>
    CombinedTree = 3,
    /// <summary>
    /// Resources diagnostic view
    /// </summary>
    Resources = 4,
}
