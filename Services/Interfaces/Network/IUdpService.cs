using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IUdpService : IDisposable
{
    bool IsOpen { get; }

    bool Open(UdpTransportOptions options, out string error);

    bool Send(string sentence, int port, string? addressOverride, out string error);

    void Close();
}
