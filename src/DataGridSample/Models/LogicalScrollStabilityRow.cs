using System.Diagnostics.CodeAnalysis;

namespace DataGridSample.Models;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed class LogicalScrollStabilityRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double Price { get; set; }
    public double Delta { get; set; }
    public double Score { get; set; }
    public double Ratio { get; set; }
}
