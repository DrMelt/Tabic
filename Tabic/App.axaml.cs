using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Tabic.Core.Models;
using Tabic.Services;
using Tabic.ViewModels;
using Tabic.Views;

namespace Tabic;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void RegisterServices()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();
        base.RegisterServices();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DialogService>();
        services.AddSingleton<ProjectService>();
        services.AddSingleton<SettingsService>();
        services.AddSingleton<TimelineData>();
        services.AddSingleton<TimelineTableViewModel>();
        services.AddSingleton<MainWindowViewModel>();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 启动时加载设置
            var settingsService = Services.GetRequiredService<SettingsService>();
            settingsService.LoadSettings();

            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}