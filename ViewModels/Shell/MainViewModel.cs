using NMEASender.Wpf.Services.Interfaces.Workflow;
using NMEASender.Wpf.ViewModels.Panels;

namespace NMEASender.Wpf.ViewModels.Shell;

public sealed class MainViewModel : IDisposable
{
    public MainViewModel(MainStateStore state, IMainWorkflowService workflow)
    {
        TopToolbar = new TopToolbarViewModel(state, workflow);
        ManualLog = new ManualLogViewModel(state, workflow);
        SentencePanel = new SentencePanelViewModel(state, workflow);

        _workflow = workflow;
    }

    private readonly IMainWorkflowService _workflow;

    public TopToolbarViewModel TopToolbar { get; }

    public ManualLogViewModel ManualLog { get; }

    public SentencePanelViewModel SentencePanel { get; }

    public void Dispose()
    {
        TopToolbar.Dispose();
        ManualLog.Dispose();
        SentencePanel.Dispose();
        _workflow.Dispose();
    }
}
