using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectUdpTransportProfileStore
{
    ProjectType ProjectType { get; }

    UdpTransportOptions Load(string configDirectory, int fallbackPort);

    void Save(string configDirectory, UdpTransportOptions options);
}
