using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectUdpTransportProfileStore : IProjectUdpTransportProfileStore
{
    public abstract ProjectType ProjectType { get; }

    public virtual UdpTransportOptions Load(string configDirectory, int fallbackPort)
    {
        return UdpTransportOptions.CreateDefault(fallbackPort);
    }

    public virtual void Save(string configDirectory, UdpTransportOptions options)
    {
    }
}
