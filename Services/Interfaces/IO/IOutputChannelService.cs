using NMEASender.Wpf.Models;

using System.IO.Ports;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IOutputChannelService : IDisposable
{
    bool IsUdpOpen { get; }

    int OpenComPortCount { get; }

    bool IsComPortOpen(string portName);

    Task<OutputOpenResult> OpenAsync(OutputOpenRequest request);

    void CloseAll();

    bool TryOpenUdp(UdpTransportOptions options, out string? error);

    void CloseUdp();

    bool TryOpenCom(
        string portName,
        int defaultBaudRate,
        IReadOnlyDictionary<string, int>? portBaudRates,
        int dataBits,
        Parity parity,
        StopBits stopBits,
        out string? error);

    bool TryWriteCom(string portName, string sentence, out string? error);

    bool TrySendUdp(string sentence, int udpPort, string? udpAddress, out string? error);

    void MarkComPortClosed(string portName);
}
