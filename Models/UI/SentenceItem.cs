using CommunityToolkit.Mvvm.ComponentModel;
using NMEASender.Wpf.Models.Core;
using System.Net.Sockets;
using System.Net;

namespace NMEASender.Wpf.Models.UI;

public sealed partial class SentenceItem : ObservableObject
{
    [ObservableProperty]
    private bool _isComEnabled;

    [ObservableProperty]
    private bool _isUdpEnabled;

    [ObservableProperty]
    private bool _isDuplicateRow;

    [ObservableProperty]
    private string _portName;

    [ObservableProperty]
    private int _udpPort;

    [ObservableProperty]
    private string _udpAddress = string.Empty;

    [ObservableProperty]
    private string _primaryText = string.Empty;

    [ObservableProperty]
    private string _secondaryText = string.Empty;

    public SentenceItem(
        NmeaSentenceId id,
        NmeaSendFlag flag,
        string label,
        string portName,
        bool isComEnabled,
        bool isUdpEnabled,
        int udpPort,
        string udpAddress = "",
        bool hasSecondary = false,
        bool isDuplicateRow = false)
    {
        Id = id;
        Flag = flag;
        Label = label;
        _portName = portName;
        _isComEnabled = isComEnabled;
        _isUdpEnabled = isUdpEnabled;
        _udpPort = NormalizeUdpPort(udpPort);
        _udpAddress = NormalizeUdpAddress(udpAddress);
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

    partial void OnUdpPortChanged(int value)
    {
        int normalized = NormalizeUdpPort(value);
        if (value != normalized)
        {
            UdpPort = normalized;
        }
    }

    partial void OnUdpAddressChanged(string value)
    {
        string normalized = NormalizeUdpAddress(value);
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            UdpAddress = normalized;
        }
    }

    private static int NormalizeUdpPort(int port)
    {
        return port is >= 1 and <= 65535 ? port : 40014;
    }

    private static string NormalizeUdpAddress(string? value)
    {
        string candidate = (value ?? string.Empty).Trim();
        if (candidate.Length == 0)
        {
            return string.Empty;
        }

        if (IPAddress.TryParse(candidate, out IPAddress? address) &&
            address.AddressFamily == AddressFamily.InterNetwork)
        {
            return candidate;
        }

        return string.Empty;
    }
}
