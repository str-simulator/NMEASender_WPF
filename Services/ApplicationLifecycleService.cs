using NMEASender.Wpf.Services.Interfaces;
using System.Windows;

namespace NMEASender.Wpf.Services;

public sealed class ApplicationLifecycleService : IApplicationLifecycleService
{
    public void RequestShutdown()
    {
        Application.Current.Shutdown();
    }
}
