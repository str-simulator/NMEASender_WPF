using NMEASender.Wpf.ViewModels;

namespace NMEASender.Wpf.Models;

public sealed class SentenceItem : ObservableObject
{
    private bool _isEnabled;
    private bool _isDuplicateRow;
    private string _portName;
    private string _primaryText = string.Empty;
    private string _secondaryText = string.Empty;

    public SentenceItem(
        NmeaSentenceId id,
        NmeaSendFlag flag,
        string label,
        string portName,
        bool isEnabled,
        bool hasSecondary = false,
        bool isDuplicateRow = false)
    {
        Id = id;
        Flag = flag;
        Label = label;
        _portName = portName;
        _isEnabled = isEnabled;
        HasSecondary = hasSecondary;
        _isDuplicateRow = isDuplicateRow;
    }

    public NmeaSentenceId Id { get; }

    public NmeaSendFlag Flag { get; }

    public string Label { get; }

    public bool HasSecondary { get; }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public bool IsDuplicateRow
    {
        get => _isDuplicateRow;
        set => SetProperty(ref _isDuplicateRow, value);
    }

    public string PortName
    {
        get => _portName;
        set => SetProperty(ref _portName, (value ?? string.Empty).Trim());
    }

    public string PrimaryText
    {
        get => _primaryText;
        set => SetProperty(ref _primaryText, value);
    }

    public string SecondaryText
    {
        get => _secondaryText;
        set => SetProperty(ref _secondaryText, value);
    }
}
