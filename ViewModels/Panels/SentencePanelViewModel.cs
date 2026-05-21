using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.ViewModels;

public sealed class SentencePanelViewModel : ObservableObject, IDisposable
{
    private readonly MainStateStore _state;
    private readonly IMainWorkflowService _workflow;

    public SentencePanelViewModel(MainStateStore state, IMainWorkflowService workflow)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));

        AddSentenceRowCommand = new RelayCommand<SentenceItem?>(_workflow.AddSentenceRow, CanAddSentenceRow);
        RemoveSentenceRowCommand = new RelayCommand<SentenceItem?>(_workflow.RemoveSentenceRow, CanRemoveSentenceRow);
        RefreshPortsCommand = new RelayCommand(_workflow.RefreshPorts);

        _state.PropertyChanged += State_PropertyChanged;
    }

    public bool AreAllComSentencesChecked
    {
        get => _state.AreAllComSentencesChecked;
        set => _state.AreAllComSentencesChecked = value;
    }

    public bool AreAllUdpSentencesChecked
    {
        get => _state.AreAllUdpSentencesChecked;
        set => _state.AreAllUdpSentencesChecked = value;
    }

    public bool IsComSettingsEditable => _state.IsComSettingsEditable;

    public ObservableCollection<string> Ports => _state.Ports;

    public ObservableCollection<SentenceItem> GpsSentences => _state.GpsSentences;

    public ObservableCollection<SentenceItem> OtherSentences => _state.OtherSentences;

    public IRelayCommand<SentenceItem?> AddSentenceRowCommand { get; }

    public IRelayCommand<SentenceItem?> RemoveSentenceRowCommand { get; }

    public IRelayCommand RefreshPortsCommand { get; }

    public void Dispose()
    {
        _state.PropertyChanged -= State_PropertyChanged;
    }

    private bool CanAddSentenceRow(SentenceItem? source)
    {
        return _state.IsComSettingsEditable && source is not null;
    }

    private bool CanRemoveSentenceRow(SentenceItem? source)
    {
        return _state.IsComSettingsEditable && source is { IsDuplicateRow: true };
    }

    private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
        if (e.PropertyName is nameof(MainStateStore.IsRunning) or nameof(MainStateStore.IsOpening))
        {
            OnPropertyChanged(nameof(IsComSettingsEditable));
            AddSentenceRowCommand.NotifyCanExecuteChanged();
            RemoveSentenceRowCommand.NotifyCanExecuteChanged();
        }
    }
}
