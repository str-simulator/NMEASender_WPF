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
