using NMEASender.Wpf.Exceptions;
using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Services.Interfaces.IO;
using NMEASender.Wpf.Services.Interfaces.Network;
using NMEASender.Wpf.Services.Interfaces.Ports;
using System.IO.Ports;

namespace NMEASender.Wpf.Services.IO;

public sealed class OutputChannelService : IOutputChannelService
{
    private readonly ISerialPortHubService _serialPortHub;
    private readonly IUdpService _udpSender;
    private readonly HashSet<string> _openPorts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _portsLock = new();

    public OutputChannelService(ISerialPortHubService serialPortHub, IUdpService udpSender)
    {
        _serialPortHub = serialPortHub ?? throw new ArgumentNullException(nameof(serialPortHub));
        _udpSender = udpSender ?? throw new ArgumentNullException(nameof(udpSender));
    }

    public bool IsUdpOpen => _udpSender.IsOpen;

    public int OpenComPortCount
    {
        get
        {
            lock (_portsLock) return _openPorts.Count;
        }
    }

    public bool IsComPortOpen(string portName)
    {
        lock (_portsLock) return _openPorts.Contains(NormalizePortName(portName));
    }

    public async Task<OutputOpenResult> OpenAsync(OutputOpenRequest request)
    {
        if (request is null)
        {
            throw new OutputChannelRequestException(nameof(request));
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

        lock (_portsLock)
        {
            foreach (PortOpenOutcome result in openResults.Where(result => result.Success))
            {
                _openPorts.Add(result.PortName);
            }
        }

        bool udpOpenSuccess = false;
        string? udpOpenError = null;

        if (request.UseUdp)
        {
            udpOpenSuccess = _udpSender.Open(request.UdpTransportOptions, out udpOpenError);
        }

        return new OutputOpenResult(openResults, udpOpenSuccess, udpOpenError);
    }

    public void CloseAll()
    {
        lock (_portsLock) _openPorts.Clear();
        _serialPortHub.CloseAll();
        _udpSender.Close();
    }

    public bool TryOpenUdp(UdpTransportOptions options, out string? error)
    {
        return _udpSender.Open(options, out error);
    }

    public void CloseUdp()
    {
        _udpSender.Close();
    }

    public bool TryOpenCom(
        string portName,
        int defaultBaudRate,
        IReadOnlyDictionary<string, int>? portBaudRates,
        int dataBits,
        Parity parity,
        StopBits stopBits,
        out string? error)
    {
        error = string.Empty;
        string normalizedPort = NormalizePortName(portName);
        if (string.IsNullOrWhiteSpace(normalizedPort))
        {
            error = new SerialPortNotSelectedException().Message;
            return false;
        }

        lock (_portsLock)
        {
            if (_openPorts.Contains(normalizedPort))
            {
                return true;
            }
        }

        _serialPortHub.Configure(defaultBaudRate, portBaudRates, dataBits, parity, stopBits);
        bool success = _serialPortHub.Open(normalizedPort, out string serialError);
        if (success)
        {
            lock (_portsLock) _openPorts.Add(normalizedPort);
            return true;
        }

        error = serialError;
        lock (_portsLock) _openPorts.Remove(normalizedPort);
        return false;
    }

    public bool TryWriteCom(string portName, string sentence, out string? error)
    {
        error = null;
        string normalizedPort = NormalizePortName(portName);

        bool isOpen;
        lock (_portsLock) isOpen = _openPorts.Contains(normalizedPort);

        if (!isOpen)
        {
            error = new SerialPortNotOpenException().Message;
            return false;
        }

        bool success = _serialPortHub.Write(normalizedPort, sentence, out error);
        if (!success)
        {
            lock (_portsLock) _openPorts.Remove(normalizedPort);
        }

        return success;
    }

    public bool TrySendUdp(string sentence, int udpPort, string? udpAddress, out string? error)
    {
        return _udpSender.Send(sentence, udpPort, udpAddress, out error);
    }

    public void MarkComPortClosed(string portName)
    {
        lock (_portsLock) _openPorts.Remove(NormalizePortName(portName));
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
