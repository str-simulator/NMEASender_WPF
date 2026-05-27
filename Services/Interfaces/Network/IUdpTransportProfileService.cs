using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Interfaces.Network;

public interface IUdpTransportProfileService
{
    UdpTransportOptions Load(ProjectType projectType, string configDirectory, int fallbackPort);

    void Save(ProjectType projectType, string configDirectory, UdpTransportOptions options);
}
