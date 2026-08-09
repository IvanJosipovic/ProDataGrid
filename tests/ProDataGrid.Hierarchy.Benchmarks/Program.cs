using System.Globalization;
using Avalonia.Controls.DataGridHierarchical;
using BenchmarkDotNet.Running;

namespace ProDataGrid.Hierarchy.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        if (TryRunDeepStackSmoke(args))
        {
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    private static bool TryRunDeepStackSmoke(string[] args)
    {
        if (args.Length == 0 ||
            !string.Equals(args[0], "--deep-stack-smoke", StringComparison.Ordinal))
        {
            return false;
        }

        if (args.Length != 2 ||
            !int.TryParse(args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int depth) ||
            depth <= 0)
        {
            throw new ArgumentException("Usage: --deep-stack-smoke <positive-depth>");
        }

        IReadOnlyList<BenchmarkNode> roots = BenchmarkTreeFactory.CreateDeepChain(depth);
        var model = new HierarchicalModel<BenchmarkNode>(new HierarchicalOptions<BenchmarkNode>
        {
            ChildrenSelector = static node => node.Children,
            IsLeafSelector = static node => node.Children.Count == 0,
            VirtualizeChildren = true,
        });
        model.SetRoots(roots);
        model.ExpandAll();

        if (model.Count != depth)
        {
            throw new InvalidOperationException(
                $"Deep-stack smoke mismatch: expected {depth}, got {model.Count}.");
        }

        Console.WriteLine(
            $"Deep-stack smoke passed: depth={depth.ToString(CultureInfo.InvariantCulture)}, rows={model.Count.ToString(CultureInfo.InvariantCulture)}.");
        return true;
    }
}
