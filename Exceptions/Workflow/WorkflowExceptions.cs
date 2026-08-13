namespace NMEASender.Wpf.Exceptions;

public abstract class WorkflowException : LayerException
{
    protected WorkflowException(string message, Exception? innerException = null)
        : base("Workflow", message, innerException)
    {
    }
}

public sealed class WorkflowStartException : WorkflowException
{
    public WorkflowStartException(Exception innerException)
        : base($"Workflow start failed: {innerException.Message}", innerException)
    {
    }
}

public sealed class WorkflowConfigSaveException : WorkflowException
{
    public WorkflowConfigSaveException(Exception innerException)
        : base($"Config save failed: {innerException.Message}", innerException)
    {
    }
}

public sealed class InvalidSentenceHzException : WorkflowException
{
    public InvalidSentenceHzException()
        : base($"Sentence Hz must be at least {NMEASender.Wpf.Models.UI.SentenceItem.MinHz}.")
    {
    }
}
