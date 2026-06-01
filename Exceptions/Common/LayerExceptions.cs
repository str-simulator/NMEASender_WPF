namespace NMEASender.Wpf.Exceptions;

public abstract class LayerException : Exception
{
    protected LayerException(string layer, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Layer = layer;
    }

    public string Layer { get; }
}
