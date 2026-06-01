using CommunityToolkit.Mvvm.ComponentModel;
using NMEASender.Wpf.Models.UI;
using System.Collections.ObjectModel;

namespace NMEASender.Wpf.ViewModels.Shell;

public sealed partial class MainStateStore : ObservableObject
{
    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private bool _isOpening;

    [ObservableProperty]
    private bool _isTestSource;

    [ObservableProperty]
    private bool _isIosSource = true;

    [ObservableProperty]
    private bool _useTrueWind;

    [ObservableProperty]
    private bool _useHdmOutput;

    [ObservableProperty]
    private bool _areAllComSentencesChecked = true;

    [ObservableProperty]
    private bool _areAllUdpSentencesChecked = true;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _defaultPort = string.Empty;

    [ObservableProperty]
    private string _udpPortText = "40014";

    [ObservableProperty]
    private string _sentenceSearchText = string.Empty;

    [ObservableProperty]
    private string _longitudeText = "129.0000";

    [ObservableProperty]
    private string _latitudeText = "35.0000";

    [ObservableProperty]
    private string _speedText = "0.0";

    [ObservableProperty]
    private string _headingText = "0.0";

    public ObservableCollection<string> Ports { get; } = new();

    public ObservableCollection<string> Logs { get; } = new();

    public ObservableCollection<SentenceItem> GpsSentences { get; } = new();

    public ObservableCollection<SentenceItem> OtherSentences { get; } = new();

    public List<SentenceItem> InternalSentences { get; } = new();

    public bool IsComSettingsEditable => !IsRunning && !IsOpening;

    public IEnumerable<SentenceItem> AllSentences()
    {
        return GpsSentences.Concat(OtherSentences).Concat(InternalSentences);
    }

    public IEnumerable<SentenceItem> ConfigurableSentences()
    {
        return GpsSentences.Concat(OtherSentences);
    }

    partial void OnIsRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsComSettingsEditable));
    }

    partial void OnIsOpeningChanged(bool value)
    {
        OnPropertyChanged(nameof(IsComSettingsEditable));
    }
}
