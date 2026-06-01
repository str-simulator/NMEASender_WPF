namespace NMEASender.Wpf.Exceptions;

public abstract class IoException : LayerException
{
    protected IoException(string message, Exception? innerException = null)
        : base("IO", message, innerException)
    {
    }
}

public sealed class OutputChannelRequestException : IoException
{
    public OutputChannelRequestException(string paramName)
        : base($"Output channel request is required. (Parameter '{paramName}')")
    {
    }
}

public abstract class SharedMemoryException : IoException
{
    protected SharedMemoryException(string message, string mapName, Exception? innerException = null)
        : base(message, innerException)
    {
        MapName = mapName;
    }

    public string MapName { get; }
}

public sealed class SharedMemoryMapNotFoundException : SharedMemoryException
{
    public SharedMemoryMapNotFoundException(string mapName, Exception? innerException = null)
        : base($"{mapName} shared memory was not found.", mapName, innerException)
    {
    }
}

public sealed class SharedMemoryAccessDeniedException : SharedMemoryException
{
    public SharedMemoryAccessDeniedException(string mapName, Exception? innerException = null)
        : base(BuildMessage(mapName, innerException), mapName, innerException)
    {
    }

    private static string BuildMessage(string mapName, Exception? innerException)
    {
        return innerException is null
            ? $"{mapName} access denied."
            : $"{mapName} access denied: {innerException.Message}";
    }
}

public sealed class SharedMemoryOpenException : SharedMemoryException
{
    public SharedMemoryOpenException(string mapName, Exception? innerException = null)
        : base(BuildMessage(mapName, innerException), mapName, innerException)
    {
    }

    private static string BuildMessage(string mapName, Exception? innerException)
    {
        return innerException is null
            ? $"{mapName} open failed."
            : $"{mapName} open failed: {innerException.Message}";
    }
}

public sealed class SharedMemoryReadException : SharedMemoryException
{
    public SharedMemoryReadException(string message, string mapName, Exception? innerException = null)
        : base(message, mapName, innerException)
    {
    }
}
