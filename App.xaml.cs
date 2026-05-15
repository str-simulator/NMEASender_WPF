using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NMEASender.Wpf.Services;
using NMEASender.Wpf.Services.Interfaces;
using NMEASender.Wpf.ViewModels;

namespace NMEASender.Wpf;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        ServiceCollection services = new();

        services.AddSingleton<INmeaSenderConfigService>(_ => NmeaSenderConfigService.Load());

        services.AddSingleton<ISerialPortHubService, SerialPortHubService>();
        services.AddSingleton<IUdpService, UdpService>();
        services.AddSingleton<INmeaSentenceBuilderService, NmeaSentenceBuilderService>();
        services.AddSingleton<IManualInputMapperService, ManualInputMapperService>();
        services.AddSingleton<IOutputChannelService, OutputChannelService>();
        services.AddSingleton<IPortBaudRateService, PortBaudRateService>();
        services.AddSingleton<INmeaTransmissionService, NmeaTransmissionService>();
        services.AddSingleton<ISharedMemoryProviderService, SharedMemoryProviderService>();
        services.AddSingleton<ISentenceComposerService, SentenceComposerService>();
        services.AddSingleton<ISentenceCatalogService, SentenceCatalogService>();
        services.AddSingleton<ISerialPortCatalogService, SerialPortCatalogService>();
        services.AddSingleton<IBaudRateSettingService, BaudRateSettingService>();
        services.AddSingleton<IApplicationLifecycleService, ApplicationLifecycleService>();

        services.AddSingleton<MainStateStore>();
        services.AddSingleton<IMainWorkflowService, MainWorkflowService>();

        services.AddTransient<MainViewModel>();
        services.AddTransient<MainWindow>();

        return services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow = Services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}
