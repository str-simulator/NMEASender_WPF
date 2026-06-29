using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.ViewModels.Shell;

namespace NMEASender.Wpf.Services.Interfaces.Workflow;

public interface IMainWorkflowService : IDisposable
{
    MainStateStore State { get; }

    IReadOnlyList<int> BaudRateOptions { get; }

    Task StartAsync();

    void Stop();

    void Exit();

    void OpenSettings();

    void OpenSummary();

    void SetData();

    void GetData();

    void ApplyDefaultPort();

    void ApplyDefaultUdpPort();

    void AddSentenceRow(SentenceItem? source);

    void RemoveSentenceRow(SentenceItem? source);

    void RefreshPorts();

    void ClearLog();
}
