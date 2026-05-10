using System.IO.Ports;
using System.Text;

namespace NMEASender.Wpf.Services;

public sealed class SerialPortHub : IDisposable
{
    private readonly Dictionary<string, SerialPort> _ports = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;
    private int _baudRate = 19200;
    private int _dataBits = 8;
    private Parity _parity = Parity.None;
    private StopBits _stopBits = StopBits.One;

    public void Configure(int baudRate, int dataBits, Parity parity, StopBits stopBits)
    {
        _baudRate = baudRate;
        _dataBits = dataBits;
        _parity = parity;
        _stopBits = stopBits;
    }

    public bool Open(string portName, out string error)
    {
        error = string.Empty;
        string normalizedPortName = NormalizePortName(portName);
        try
        {
            if (_disposed)
            {
                error = "Serial port hub is disposed.";
                return false;
            }

            if (_ports.TryGetValue(normalizedPortName, out SerialPort? existingPort) && existingPort.IsOpen)
            {
                return true;
            }

            existingPort?.Dispose();
            _ports.Remove(normalizedPortName);

            SerialPort port = CreatePort(normalizedPortName);
            port.Open();
            _ports[normalizedPortName] = port;
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            ClosePort(normalizedPortName);
            return false;
        }
    }

    public bool Write(string portName, string sentence, out string error)
    {
        error = string.Empty;
        string normalizedPortName = NormalizePortName(portName);
        try
        {
            if (!_ports.TryGetValue(normalizedPortName, out SerialPort? port) || !port.IsOpen)
            {
                error = "COM port is not open.";
                return false;
            }

            port.Write(sentence);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            ClosePort(normalizedPortName);
            return false;
        }
    }

    public void CloseAll()
    {
        foreach (SerialPort port in _ports.Values)
        {
            try
            {
                if (port.IsOpen)
                {
                    port.Close();
                }
            }
            catch
            {
            }

            port.Dispose();
        }

        _ports.Clear();
    }

    public void Dispose()
    {
        CloseAll();
        _disposed = true;
    }

    private SerialPort CreatePort(string portName)
    {
        return new SerialPort(portName, _baudRate, _parity, _dataBits, _stopBits)
        {
            Encoding = Encoding.ASCII,
            NewLine = "\r\n",
            ReadTimeout = 1000,
            WriteTimeout = 1000,
            DtrEnable = false,
            RtsEnable = false
        };
    }

    private void ClosePort(string portName)
    {
        if (!_ports.Remove(portName, out SerialPort? port))
        {
            return;
        }

        try
        {
            if (port.IsOpen)
            {
                port.Close();
            }
        }
        catch
        {
        }

        port.Dispose();
    }

    private static string NormalizePortName(string portName)
    {
        return (portName ?? string.Empty).Trim().ToUpperInvariant();
    }
}
