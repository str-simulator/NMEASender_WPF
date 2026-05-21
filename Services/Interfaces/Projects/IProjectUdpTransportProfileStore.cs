using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IProjectUdpTransportProfileStore
{
    ProjectType ProjectType { get; }

    UdpTransportOptions Load(string configDirectory, int fallbackPort);

    void Save(string configDirectory, UdpTransportOptions options);
}
