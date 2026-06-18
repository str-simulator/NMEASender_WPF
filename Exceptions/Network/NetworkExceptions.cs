namespace NMEASender.Wpf.Exceptions;

public abstract class NetworkException : LayerException
{
    protected NetworkException(string message, Exception? innerException = null)
        : base("Network", message, innerException)
    {
    }
}

public sealed class UdpTransportProfileRegistrationException : NetworkException
{
    public UdpTransportProfileRegistrationException(string message)
        : base(message)
    {
    }
}

public sealed class UdpOpenException : NetworkException
{
    public UdpOpenException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class UdpSendException : NetworkException
{
    public UdpSendException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class InvalidMulticastAddressException : NetworkException
{
    public InvalidMulticastAddressException()
        : base("Multicast address must be a valid IPv4 address.")
    {
    }
}

public sealed class MulticastAddressRangeException : NetworkException
{
    public MulticastAddressRangeException()
        : base("Multicast address must be in 224.0.0.0 - 239.255.255.255.")
    {
    }
}

public sealed class InvalidUdpPortException : NetworkException
{
    public InvalidUdpPortException()
        : base("UDP port must be between 1 and 65535.")
    {
    }
}
