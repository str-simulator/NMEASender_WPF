using NMEASender.Wpf.Models.Network;

namespace NMEASender.Wpf.Services.Interfaces.Network;

public interface IUdpService : IDisposable
{
    bool IsOpen { get; }

    bool Open(UdpTransportOptions options, out string error);

    bool Send(string sentence, int port, string? addressOverride, out string error);

    void Close();
}
