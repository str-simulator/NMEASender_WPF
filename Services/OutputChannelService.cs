using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services;

public sealed class OutputChannelService : IOutputChannelService
{
    private readonly ISerialPortHubService _serialPortHub;
    private readonly IUdpService _udpSender;
    private readonly HashSet<string> _openPorts = new(StringComparer.OrdinalIgnoreCase);

    public OutputChannelService(ISerialPortHubService serialPortHub, IUdpService udpSender)
    {
        _serialPortHub = serialPortHub ?? throw new ArgumentNullException(nameof(serialPortHub));
        _udpSender = udpSender ?? throw new ArgumentNullException(nameof(udpSender));
    }

    public bool IsUdpOpen => _udpSender.IsOpen;

    public int OpenComPortCount => _openPorts.Count;

    public bool IsComPortOpen(string portName)
    {
        return _openPorts.Contains(NormalizePortName(portName));
    }

    public async Task<OutputOpenResult> OpenAsync(OutputOpenRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        CloseAll();

        _serialPortHub.Configure(
            request.DefaultBaudRate,
            request.PortBaudRates,
            request.DataBits,
            request.Parity,
            request.StopBits);

        List<string> enabledPorts = request.EnabledPorts
            .Where(port => !string.IsNullOrWhiteSpace(port))
            .Select(NormalizePortName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<PortOpenOutcome> openResults = await Task.Run(() => enabledPorts
            .Select(portName =>
            {
                bool success = _serialPortHub.Open(portName, out string error);
                return new PortOpenOutcome(portName, success, error);
            })
            .ToList());

        foreach (PortOpenOutcome result in openResults.Where(result => result.Success))
        {
            _openPorts.Add(result.PortName);
        }

        bool udpOpenSuccess = false;
        string? udpOpenError = null;

        if (request.UseUdp)
        {
            udpOpenSuccess = _udpSender.Open(request.UdpPort, out udpOpenError);
        }

        return new OutputOpenResult(openResults, udpOpenSuccess, udpOpenError);
    }

    public void CloseAll()
    {
        _serialPortHub.CloseAll();
        _udpSender.Close();
        _openPorts.Clear();
    }

    public bool TryOpenUdp(int udpPort, out string? error)
    {
        return _udpSender.Open(udpPort, out error);
    }

    public void CloseUdp()
    {
        _udpSender.Close();
    }

    public bool TryWriteCom(string portName, string sentence, out string? error)
    {
        string normalizedPort = NormalizePortName(portName);
        if (!_openPorts.Contains(normalizedPort))
        {
            error = "COM port is not open.";
            return false;
        }

        bool success = _serialPortHub.Write(normalizedPort, sentence, out error);
        if (!success)
        {
            _openPorts.Remove(normalizedPort);
        }

        return success;
    }

    public bool TrySendUdp(string sentence, out string? error)
    {
        return _udpSender.Send(sentence, out error);
    }

    public void MarkComPortClosed(string portName)
    {
        _openPorts.Remove(NormalizePortName(portName));
    }

    public void Dispose()
    {
        CloseAll();
    }

    private static string NormalizePortName(string portName)
    {
        return (portName ?? string.Empty).Trim().ToUpperInvariant();
    }
}
