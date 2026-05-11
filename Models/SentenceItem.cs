using CommunityToolkit.Mvvm.ComponentModel;

namespace NMEASender.Wpf.Models;

public sealed partial class SentenceItem : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private bool _isDuplicateRow;

    [ObservableProperty]
    private string _portName;

    [ObservableProperty]
    private string _primaryText = string.Empty;

    [ObservableProperty]
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

    partial void OnPortNameChanged(string value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            PortName = normalized;
        }
    }
}
