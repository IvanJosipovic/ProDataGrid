// Copyright (c) Wieslaw Soltes. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for details.

using BenchmarkDotNet.Running;

namespace ProDataGrid.SourceGeneration.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length == 1 && string.Equals(args[0], "--validate", StringComparison.Ordinal))
        {
            BenchmarkCorrectness.Validate();
            return 0;
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return 0;
    }
}
