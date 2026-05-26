using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services;
using NMEASender.Wpf.Services.Network;
using NMEASender.Wpf.Services.Interfaces;
using NMEASender.Wpf.Services.Projects;
using NMEASender.Wpf.Services.Projects.PS000;
using NMEASender.Wpf.Services.Projects.PS2603;
using NMEASender.Wpf.Services.Projects.PS2404A;
using NMEASender.Wpf.ViewModels;
using NMEASender.Wpf.Services.Transmission;

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

        services.AddSingleton<IProjectSendFlagCodec>(_ => new DefaultProjectSendFlagCodec(ProjectType.PS2603));
        services.AddSingleton<IProjectSendFlagCodec>(_ => new DefaultProjectSendFlagCodec(ProjectType.PS000));
        services.AddSingleton<IProjectSendFlagCodec, PS2404ASendFlagCodec>();

        services.AddSingleton<IProjectUdpTransportProfileStore>(_ => new DefaultProjectUdpTransportProfileStore(ProjectType.PS2603));
        services.AddSingleton<IProjectUdpTransportProfileStore>(_ => new DefaultProjectUdpTransportProfileStore(ProjectType.PS000));
        services.AddSingleton<IProjectUdpTransportProfileStore, PS2404AUdpTransportProfileStore>();
        services.AddSingleton<IUdpTransportProfileService, UdpTransportProfileService>();

        services.AddSingleton<INmeaSenderConfigService>(sp =>
            NmeaSenderConfigService.Load(
                sp.GetRequiredService<IUdpTransportProfileService>(),
                sp.GetServices<IProjectSendFlagCodec>()));

        services.AddSingleton<ISerialPortHubService, SerialPortHubService>();
        services.AddSingleton<IUdpService, UdpService>();
        services.AddSingleton<IProjectNmeaSentenceBuilder, Ps2603NmeaSentenceBuilder>();
        services.AddSingleton<IProjectNmeaSentenceBuilder, Ps000NmeaSentenceBuilder>();
        services.AddSingleton<IProjectNmeaSentenceBuilder, PS2404ANmeaSentenceBuilder>();
        services.AddSingleton<INmeaSentenceBuilderService, NmeaSentenceBuilderService>();

        services.AddSingleton<IProjectSentenceComposerProfile>(_ => new DefaultProjectSentenceComposerProfile(ProjectType.PS2603));
        services.AddSingleton<IProjectSentenceComposerProfile>(_ => new DefaultProjectSentenceComposerProfile(ProjectType.PS000));
        services.AddSingleton<IProjectSentenceComposerProfile, PS2404ASentenceComposerProfile>();
        services.AddSingleton<IManualInputMapperService, ManualInputMapperService>();
        services.AddSingleton<IOutputChannelService, OutputChannelService>();

        services.AddSingleton<IProjectSentenceFramePolicy, Ps2603SentenceFramePolicy>();
        services.AddSingleton<IProjectSentenceFramePolicy>(_ => new DefaultProjectSentenceFramePolicy(ProjectType.PS000));
        services.AddSingleton<IProjectSentenceFramePolicy, PS2404ASentenceFramePolicy>();
        services.AddSingleton<IProjectSentenceFrameService, ProjectSentenceFrameService>();

        services.AddSingleton<IProjectSentenceCatalogPolicy>(_ => new DefaultProjectSentenceCatalogPolicy(ProjectType.PS2603));
        services.AddSingleton<IProjectSentenceCatalogPolicy>(_ => new DefaultProjectSentenceCatalogPolicy(ProjectType.PS000));
        services.AddSingleton<IProjectSentenceCatalogPolicy, PS2404ASentenceCatalogPolicy>();
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
