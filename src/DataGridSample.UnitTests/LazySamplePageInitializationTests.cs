using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using DataGridSample.Pages;
using DataGridSample.ViewModels;
using Xunit;

namespace DataGridSample.Tests;

public sealed class LazySamplePageInitializationTests
{
    [AvaloniaFact]
    public void AllCatalogPages_DoNotCreateDataContext_InConstructor()
    {
        foreach ((string pageName, Func<Control> pageFactory) in SamplePageCatalog.All)
        {
            var page = pageFactory();
            Assert.True(page.DataContext is null, $"{pageName} should not create DataContext in constructor.");
        }
    }

    [AvaloniaFact]
    public void HeavyPages_DoNotCreateDataContext_InConstructor()
    {
        foreach (Func<Control> pageFactory in LazyPages())
        {
            var page = pageFactory();
            Assert.True(page.DataContext is null, $"{page.GetType().Name} should not create DataContext in constructor.");
        }
    }

    [AvaloniaFact]
    public void HeavyPages_CreateDataContext_WhenAttached()
    {
        foreach ((Func<Control> pageFactory, Type expectedViewModelType) in AttachableLazyPages())
        {
            var page = pageFactory();
            Assert.True(page.DataContext is null, $"{page.GetType().Name} should be lazy before attach.");

            var window = CreateHostWindow(page);
            try
            {
                window.Show();
                PumpLayout(window);
                Assert.IsType(expectedViewModelType, page.DataContext);
            }
            finally
            {
                window.Close();
            }
        }
    }

    private static IEnumerable<Func<Control>> LazyPages()
    {
        yield return () => new PixelColumnsPage();
        yield return () => new FrozenColumnsPage();
        yield return () => new RightFrozenColumnsPage();
        yield return () => new RecycleDiagnosticsPage();
        yield return () => new LargeUniformPage();
    }

    private static IEnumerable<(Func<Control> pageFactory, Type expectedViewModelType)> AttachableLazyPages()
    {
        yield return (() => new PixelColumnsPage(), typeof(PixelColumnsViewModel));
        yield return (() => new FrozenColumnsPage(), typeof(FrozenColumnsViewModel));
        yield return (() => new RightFrozenColumnsPage(), typeof(RightFrozenColumnsViewModel));
        yield return (() => new RecycleDiagnosticsPage(), typeof(RecycleDiagnosticsViewModel));
    }

    private static Window CreateHostWindow(Control content)
    {
        var window = new Window
        {
            Width = 1024,
            Height = 720,
            Content = content
        };
        window.ApplySampleTheme();
        return window;
    }

    private static void PumpLayout(Control control)
    {
        Dispatcher.UIThread.RunJobs();
        control.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }
}
