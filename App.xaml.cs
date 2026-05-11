using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using NMEASender.Wpf.Services;
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

        services.AddSingleton(_ => NmeaSenderConfig.Load());

        services.AddSingleton<SerialPortHub>();
        services.AddSingleton<UdpBroadcastSender>();
        services.AddSingleton<SharedMemoryNmeaDataProvider>();
        services.AddSingleton<SentenceComposerService>();
        services.AddSingleton<SentenceCatalogService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

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
