namespace NMEASender.Wpf.Exceptions;

public abstract class TransmissionException : LayerException
{
    protected TransmissionException(string message, Exception? innerException = null)
        : base("Transmission", message, innerException)
    {
    }
}

public sealed class TransmissionServiceRegistrationException : TransmissionException
{
    public TransmissionServiceRegistrationException(string message)
        : base(message)
    {
    }
}

public sealed class TransmissionContextException : TransmissionException
{
    public TransmissionContextException(string paramName)
        : base($"Transmission context is required. (Parameter '{paramName}')")
    {
    }
}
