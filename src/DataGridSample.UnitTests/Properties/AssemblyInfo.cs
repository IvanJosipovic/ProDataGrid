using Xunit;

// Avalonia Headless owns process-global dispatcher and compositor state.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
