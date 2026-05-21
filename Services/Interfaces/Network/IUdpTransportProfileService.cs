using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IUdpTransportProfileService
{
    UdpTransportOptions Load(ProjectType projectType, string configDirectory, int fallbackPort);

    void Save(ProjectType projectType, string configDirectory, UdpTransportOptions options);
}
