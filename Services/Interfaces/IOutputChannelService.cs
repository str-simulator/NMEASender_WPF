using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IOutputChannelService : IDisposable
{
    bool IsUdpOpen { get; }

    int OpenComPortCount { get; }

    bool IsComPortOpen(string portName);

    Task<OutputOpenResult> OpenAsync(OutputOpenRequest request);

    void CloseAll();

    bool TryOpenUdp(int udpPort, out string? error);

    void CloseUdp();

    bool TryWriteCom(string portName, string sentence, out string? error);

    bool TrySendUdp(string sentence, int udpPort, out string? error);

    void MarkComPortClosed(string portName);
}
