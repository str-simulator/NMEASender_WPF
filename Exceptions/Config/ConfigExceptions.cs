using NMEASender.Wpf.Services.Interfaces.Config;

namespace NMEASender.Wpf.Exceptions;

public abstract class ConfigException : LayerException
{
    protected ConfigException(string message, Exception? innerException = null)
        : base("Config", message, innerException)
    {
    }
}

public sealed class ConfigServiceRegistrationException : ConfigException
{
    public ConfigServiceRegistrationException(string message)
        : base(message)
    {
    }
}

public sealed class UnsupportedIniImplementationException : ConfigException
{
    public UnsupportedIniImplementationException(IIniFileService implementation)
        : base(BuildMessage(implementation))
    {
    }

    private static string BuildMessage(IIniFileService implementation)
    {
        string typeName = implementation?.GetType().FullName ?? "<null>";
        return $"Unsupported INI implementation: {typeName}.";
    }
}
