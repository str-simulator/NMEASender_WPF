using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Workflow;
using NMEASender.Wpf.Services.Interfaces.Search;
using NMEASender.Wpf.ViewModels.Shell;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Data;

namespace NMEASender.Wpf.ViewModels.Panels;

public sealed class SentencePanelViewModel : ObservableObject, IDisposable
{
    private readonly MainStateStore _state;
    private readonly IMainWorkflowService _workflow;
    private readonly ISentenceSearchService _sentenceSearchService;
    private readonly ICollectionView _gpsSentencesView;
    private readonly ICollectionView _otherSentencesView;

    public SentencePanelViewModel(
        MainStateStore state,
        IMainWorkflowService workflow,
        ISentenceSearchService sentenceSearchService)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        _sentenceSearchService = sentenceSearchService ?? throw new ArgumentNullException(nameof(sentenceSearchService));

        AddSentenceRowCommand = new RelayCommand<SentenceItem?>(_workflow.AddSentenceRow, CanAddSentenceRow);
        RemoveSentenceRowCommand = new RelayCommand<SentenceItem?>(_workflow.RemoveSentenceRow, CanRemoveSentenceRow);
        RefreshPortsCommand = new RelayCommand(_workflow.RefreshPorts);

        _gpsSentencesView = CollectionViewSource.GetDefaultView(_state.GpsSentences);
        _otherSentencesView = CollectionViewSource.GetDefaultView(_state.OtherSentences);
        _gpsSentencesView.Filter = FilterSentence;
        _otherSentencesView.Filter = FilterSentence;

        HookSentenceCollections();
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

    public ICollectionView GpsSentencesView => _gpsSentencesView;

    public ICollectionView OtherSentencesView => _otherSentencesView;

    public IRelayCommand<SentenceItem?> AddSentenceRowCommand { get; }

    public IRelayCommand<SentenceItem?> RemoveSentenceRowCommand { get; }

    public IRelayCommand RefreshPortsCommand { get; }

    public void Dispose()
    {
        UnhookSentenceCollections();
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
        if (e.PropertyName == nameof(MainStateStore.SentenceSearchText))
        {
            RefreshSentenceFilters();
        }

        if (e.PropertyName is nameof(MainStateStore.IsRunning) or nameof(MainStateStore.IsOpening))
        {
            OnPropertyChanged(nameof(IsComSettingsEditable));
            AddSentenceRowCommand.NotifyCanExecuteChanged();
            RemoveSentenceRowCommand.NotifyCanExecuteChanged();
        }
    }

    private bool FilterSentence(object item)
    {
        return _sentenceSearchService.MatchesSentence(item as SentenceItem, _state.SentenceSearchText);
    }

    private void RefreshSentenceFilters()
    {
        _gpsSentencesView.Refresh();
        _otherSentencesView.Refresh();
    }

    private void HookSentenceCollections()
    {
        _state.GpsSentences.CollectionChanged += Sentences_CollectionChanged;
        _state.OtherSentences.CollectionChanged += Sentences_CollectionChanged;

        foreach (SentenceItem sentence in _state.ConfigurableSentences())
        {
            sentence.PropertyChanged += Sentence_PropertyChanged;
        }
    }

    private void UnhookSentenceCollections()
    {
        _state.GpsSentences.CollectionChanged -= Sentences_CollectionChanged;
        _state.OtherSentences.CollectionChanged -= Sentences_CollectionChanged;

        foreach (SentenceItem sentence in _state.ConfigurableSentences())
        {
            sentence.PropertyChanged -= Sentence_PropertyChanged;
        }
    }

    private void Sentences_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (SentenceItem sentence in e.OldItems.OfType<SentenceItem>())
            {
                sentence.PropertyChanged -= Sentence_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (SentenceItem sentence in e.NewItems.OfType<SentenceItem>())
            {
                sentence.PropertyChanged += Sentence_PropertyChanged;
            }
        }
    }

    private void Sentence_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_state.SentenceSearchText))
        {
            return;
        }

        if (e.PropertyName is nameof(SentenceItem.PrimaryText) or nameof(SentenceItem.SecondaryText))
        {
            RefreshSentenceFilters();
        }
    }
}
