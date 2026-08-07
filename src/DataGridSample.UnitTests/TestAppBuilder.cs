using System;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;
using ReactiveUI.Avalonia;

[assembly: Avalonia.Headless.AvaloniaTestApplication(typeof(DataGridSample.Tests.UnitTestAppBuilder))]

namespace DataGridSample.Tests;

internal static class UnitTestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        AppContext.SetSwitch("ProDataGrid.Diagnostics.IsEnabled", true);

        bool captureScreenshots = !string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable("AVALONIA_SCREENSHOT_DIR"));
        var options = new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = !captureScreenshots
        };

        AppBuilder builder = AppBuilder.Configure<UnitTestApp>();
        if (captureScreenshots)
        {
            builder = builder.UseSkia();
        }

        return builder
            .UseHeadless(options)
            .UseReactiveUI(static _ => { });
    }
}

internal sealed class UnitTestApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://Avalonia.Controls.DataGrid/Themes/"))
        {
            Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.v2.xaml")
        });
    }
}
