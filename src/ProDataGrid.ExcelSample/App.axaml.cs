using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ProDataGrid.ExcelSample.ViewModels;

namespace ProDataGrid.ExcelSample;

public sealed class App : Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = ConfigureServices();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = _services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _services.GetRequiredService<WorkbookViewModel>();
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<WorkbookViewModel>();
        services.AddTransient<MainWindow>();
        return services.BuildServiceProvider();
    }
}
