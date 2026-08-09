namespace DataGridSample.Models;

public sealed record OptimizedCellPathOption(
    string Key,
    string Name,
    string Container,
    string Configuration,
    string Description,
    bool UsesOptimizedTheme);
