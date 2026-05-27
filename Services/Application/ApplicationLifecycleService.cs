using NMEASender.Wpf.Services.Interfaces.Application;
using System.Windows;

namespace NMEASender.Wpf.Services.Application;

public sealed class ApplicationLifecycleService : IApplicationLifecycleService
{
    public void RequestShutdown()
    {
        System.Windows.Application.Current.Shutdown();
    }
}
