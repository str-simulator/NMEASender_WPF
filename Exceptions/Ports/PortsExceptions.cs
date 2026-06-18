namespace NMEASender.Wpf.Exceptions;

public abstract class PortsException : LayerException
{
    protected PortsException(string message, Exception? innerException = null)
        : base("Ports", message, innerException)
    {
    }
}

public sealed class SerialPortCatalogException : PortsException
{
    public SerialPortCatalogException(Exception innerException)
        : base($"COM port scan failed: {innerException.Message}", innerException)
    {
    }
}

public sealed class SerialPortOpenException : PortsException
{
    public SerialPortOpenException(string portName, string message, Exception? innerException = null)
        : base($"{NormalizePortName(portName)} open failed: {message}", innerException)
    {
    }

    private static string NormalizePortName(string portName)
    {
        return string.IsNullOrWhiteSpace(portName) ? "<unknown>" : portName.Trim();
    }
}

public sealed class SerialPortWriteException : PortsException
{
    public SerialPortWriteException(string portName, string message, Exception? innerException = null)
        : base($"{NormalizePortName(portName)} write failed: {message}", innerException)
    {
    }

    private static string NormalizePortName(string portName)
    {
        return string.IsNullOrWhiteSpace(portName) ? "<unknown>" : portName.Trim();
    }
}

public sealed class SerialPortNotSelectedException : PortsException
{
    public SerialPortNotSelectedException()
        : base("COM port is not selected.")
    {
    }
}

public sealed class SerialPortNotOpenException : PortsException
{
    public SerialPortNotOpenException()
        : base("COM port is not open.")
    {
    }
}
