using System.IO.Ports;

namespace NMEASender.Wpf.Services.Interfaces.Ports;

public interface ISerialPortHubService : IDisposable
{
    void Configure(
        int defaultBaudRate,
        IReadOnlyDictionary<string, int>? portBaudRates,
        int dataBits,
        Parity parity,
        StopBits stopBits);

    bool Open(string portName, out string error);

    bool Write(string portName, string sentence, out string error);

    void CloseAll();
}
