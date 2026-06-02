using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Search;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace NMEASender.Wpf.ViewModels.Dialogs;

public sealed partial class PortBaudRateSettingsViewModel : ObservableObject
{
    private static readonly int[] DefaultBaudRates = [1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200];
    private readonly int _broadcastPort;
    private readonly int _multicastPortNo;
    private readonly int _multicastSendPort;
    private readonly int _multicastTtl;
    private readonly string _defaultMulticastAddress;
    private readonly string _multicastInterfaceAddress;
    private readonly bool _useRequestedPort;
    private readonly bool _supportsPerSentenceMulticastAddress;
    private readonly ISentenceSearchService _sentenceSearchService;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBroadcastMode))]
    [NotifyPropertyChangedFor(nameof(IsMulticastMode))]
    [NotifyPropertyChangedFor(nameof(IsMulticastAddressEditable))]
    [NotifyPropertyChangedFor(nameof(IsSentenceMulticastAddressEditable))]
    private UdpTransportMode _selectedUdpMode = UdpTransportMode.Broadcast;

    [ObservableProperty]
    private string _multicastAddress = "225.0.0.0";

    [ObservableProperty]
    private string _udpPortText = "40014";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSentenceUdpSearchText))]
    private string _sentenceUdpSearchText = string.Empty;

    public PortBaudRateSettingsViewModel(
        ISentenceSearchService sentenceSearchService,
        IReadOnlyDictionary<string, int> portBaudRates,
        IReadOnlyList<int>? baudRateOptions = null,
        IReadOnlyList<SentenceUdpPortSetting>? sentenceUdpPortSettings = null,
        int currentUdpPort = 40014,
        UdpTransportOptions? udpTransportOptions = null,
        bool supportsPerSentenceMulticastAddress = false)
    {
        _sentenceSearchService = sentenceSearchService ?? throw new ArgumentNullException(nameof(sentenceSearchService));
        _supportsPerSentenceMulticastAddress = supportsPerSentenceMulticastAddress;
        int normalizedUdpPort = NormalizeUdpPort(currentUdpPort);
        UdpTransportOptions effectiveUdpOptions = (udpTransportOptions ?? UdpTransportOptions.CreateDefault())
            .WithFallbackPort(normalizedUdpPort);
        _broadcastPort = effectiveUdpOptions.BroadcastPort;
        _multicastPortNo = effectiveUdpOptions.MulticastPortNo;
        _multicastSendPort = effectiveUdpOptions.MulticastSendPort;
        _multicastTtl = effectiveUdpOptions.MulticastTtl;
        _defaultMulticastAddress = effectiveUdpOptions.MulticastAddress;
        _multicastInterfaceAddress = effectiveUdpOptions.MulticastInterfaceAddress;
        _useRequestedPort = effectiveUdpOptions.UseRequestedPort;
        UdpPortText = normalizedUdpPort.ToString(CultureInfo.InvariantCulture);

        BaudRateOptions = (baudRateOptions is { Count: > 0 } ? baudRateOptions : DefaultBaudRates)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        SelectedUdpMode = effectiveUdpOptions.Mode == UdpTransportMode.Multicast
            ? UdpTransportMode.Multicast
            : UdpTransportMode.Broadcast;
        MulticastAddress = effectiveUdpOptions.MulticastAddress;

        foreach ((string portName, int baudRate) in portBaudRates.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            PortBaudRates.Add(new PortBaudRateItem(portName, baudRate));
        }

        if (sentenceUdpPortSettings is null)
        {
            return;
        }

        foreach (SentenceUdpPortSetting setting in sentenceUdpPortSettings)
        {
            SentenceUdpPorts.Add(new SentenceUdpPortItem(setting.RowKey, setting.SentenceLabel, setting.UdpPort, setting.UdpAddress));
        }

        HookSentenceUdpRows();
        RefreshSentenceUdpFilter();
    }

    public ObservableCollection<PortBaudRateItem> PortBaudRates { get; } = new();

    public ObservableCollection<SentenceUdpPortItem> SentenceUdpPorts { get; } = new();

    public ObservableCollection<SentenceUdpPortItem> FilteredSentenceUdpPorts { get; } = new();

    public IReadOnlyList<int> BaudRateOptions { get; }

    public IReadOnlyDictionary<string, int> Result { get; private set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, int> SentenceUdpPortResult { get; private set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> SentenceUdpAddressResult { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public int UdpPortResult { get; private set; } = 40014;

    public UdpTransportOptions UdpTransportResult { get; private set; } = UdpTransportOptions.CreateDefault(40014);

    public bool IsBroadcastMode
    {
        get => SelectedUdpMode == UdpTransportMode.Broadcast;
        set
        {
            if (value)
            {
                SelectedUdpMode = UdpTransportMode.Broadcast;
            }
        }
    }

    public bool IsMulticastMode
    {
        get => SelectedUdpMode == UdpTransportMode.Multicast;
        set
        {
            if (value)
            {
                SelectedUdpMode = UdpTransportMode.Multicast;
            }
        }
    }

    public bool IsMulticastAddressEditable => SelectedUdpMode == UdpTransportMode.Multicast;

    public bool IsSentenceMulticastAddressEditable => _supportsPerSentenceMulticastAddress && SelectedUdpMode == UdpTransportMode.Multicast;

    public bool HasSentenceUdpSearchText => !string.IsNullOrWhiteSpace(SentenceUdpSearchText);

    public event EventHandler<bool>? CloseRequested;

    partial void OnMulticastAddressChanged(string value)
    {
        ApplyMulticastAddressToSentenceRows(value);
    }

    partial void OnSentenceUdpSearchTextChanged(string value)
    {
        RefreshSentenceUdpFilter();
    }

    partial void OnUdpPortTextChanged(string value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        if (!string.Equals(value, trimmed, StringComparison.Ordinal))
        {
            UdpPortText = trimmed;
        }
    }

    partial void OnSelectedUdpModeChanged(UdpTransportMode value)
    {
        if (value == UdpTransportMode.Multicast)
        {
            ApplyMulticastAddressToSentenceRows(MulticastAddress);
        }
    }

    [RelayCommand]
    private void Save()
    {
        Dictionary<string, int> baudRateResult = new(StringComparer.OrdinalIgnoreCase);
        if (!TryParseUdpPort(UdpPortText, out int udpPort))
        {
            ValidationMessage = "UDP port must be between 1 and 65535.";
            return;
        }

        foreach (PortBaudRateItem item in PortBaudRates)
        {
            if (string.IsNullOrWhiteSpace(item.PortName))
            {
                continue;
            }

            if (item.BaudRate <= 0)
            {
                ValidationMessage = $"{item.PortName} baud rate is invalid.";
                return;
            }

            baudRateResult[item.PortName] = item.BaudRate;
        }

        Dictionary<string, int> udpPortResult = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> udpAddressResult = new(StringComparer.OrdinalIgnoreCase);
        foreach (SentenceUdpPortItem item in SentenceUdpPorts)
        {
            if (string.IsNullOrWhiteSpace(item.RowKey))
            {
                continue;
            }

            if (item.UdpPort is < 1 or > 65535)
            {
                ValidationMessage = $"{item.SentenceLabel} UDP port must be between 1 and 65535.";
                return;
            }

            udpPortResult[item.RowKey] = item.UdpPort;

            string candidateAddress = (item.UdpAddress ?? string.Empty).Trim();
            if (IsSentenceMulticastAddressEditable)
            {
                if (!TryNormalizeMulticastAddress(candidateAddress, out string normalizedAddress))
                {
                    ValidationMessage = $"{item.SentenceLabel} multicast address is invalid.";
                    return;
                }

                udpAddressResult[item.RowKey] = normalizedAddress;
                continue;
            }

            udpAddressResult[item.RowKey] = UdpTransportOptions.NormalizeAddress(candidateAddress, _defaultMulticastAddress);
        }

        UdpTransportMode selectedMode = SelectedUdpMode == UdpTransportMode.Multicast
            ? UdpTransportMode.Multicast
            : UdpTransportMode.Broadcast;
        string resolvedMulticastAddress = UdpTransportOptions.NormalizeAddress(MulticastAddress, _defaultMulticastAddress);
        if (selectedMode == UdpTransportMode.Multicast)
        {
            string candidate = (MulticastAddress ?? string.Empty).Trim();
            if (!IPAddress.TryParse(candidate, out IPAddress? address) ||
                address.AddressFamily != AddressFamily.InterNetwork)
            {
                ValidationMessage = "Multicast address must be a valid IPv4 address.";
                return;
            }

            if (!IsMulticastAddress(address))
            {
                ValidationMessage = "Multicast address must be in 224.0.0.0 - 239.255.255.255.";
                return;
            }

            resolvedMulticastAddress = candidate;
        }

        UdpTransportResult = new UdpTransportOptions(
            selectedMode,
            _broadcastPort,
            _multicastPortNo,
            _multicastSendPort,
            resolvedMulticastAddress,
            _multicastTtl,
            _multicastInterfaceAddress,
            _useRequestedPort).WithFallbackPort(udpPort);

        ValidationMessage = string.Empty;
        Result = baudRateResult;
        SentenceUdpPortResult = udpPortResult;
        SentenceUdpAddressResult = udpAddressResult;
        UdpPortResult = udpPort;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    [RelayCommand]
    private void ClearSentenceUdpSearch()
    {
        SentenceUdpSearchText = string.Empty;
    }

    [RelayCommand]
    private void ApplyUdpPortToAll()
    {
        if (!TryParseUdpPort(UdpPortText, out int udpPort))
        {
            ValidationMessage = "UDP port must be between 1 and 65535.";
            return;
        }

        foreach (SentenceUdpPortItem row in SentenceUdpPorts)
        {
            row.UdpPort = udpPort;
        }

        ValidationMessage = string.Empty;
    }

    private static bool IsMulticastAddress(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] is >= 224 and <= 239;
    }

    private static bool TryParseUdpPort(string value, out int port)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out port) &&
               port is >= 1 and <= 65535;
    }

    private static int NormalizeUdpPort(int port)
    {
        return port is >= 1 and <= 65535 ? port : 40014;
    }

    private static bool TryNormalizeMulticastAddress(string? value, out string normalizedAddress)
    {
        normalizedAddress = string.Empty;
        string candidate = (value ?? string.Empty).Trim();
        if (!IPAddress.TryParse(candidate, out IPAddress? address) ||
            address.AddressFamily != AddressFamily.InterNetwork ||
            !IsMulticastAddress(address))
        {
            return false;
        }

        normalizedAddress = candidate;
        return true;
    }

    private void ApplyMulticastAddressToSentenceRows(string? rawAddress)
    {
        if (!IsSentenceMulticastAddressEditable)
        {
            return;
        }

        if (!TryNormalizeMulticastAddress(rawAddress, out string normalizedAddress))
        {
            return;
        }

        foreach (SentenceUdpPortItem row in SentenceUdpPorts)
        {
            row.UdpAddress = normalizedAddress;
        }
    }

    private void RefreshSentenceUdpFilter()
    {
        IEnumerable<SentenceUdpPortItem> query = _sentenceSearchService.FilterSentenceUdpPorts(SentenceUdpPorts, SentenceUdpSearchText);

        FilteredSentenceUdpPorts.Clear();
        foreach (SentenceUdpPortItem item in query)
        {
            FilteredSentenceUdpPorts.Add(item);
        }
    }

    private void HookSentenceUdpRows()
    {
        SentenceUdpPorts.CollectionChanged += SentenceUdpPorts_CollectionChanged;
        foreach (SentenceUdpPortItem row in SentenceUdpPorts)
        {
            row.PropertyChanged += SentenceUdpRow_PropertyChanged;
        }
    }

    private void SentenceUdpPorts_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (SentenceUdpPortItem row in e.OldItems.OfType<SentenceUdpPortItem>())
            {
                row.PropertyChanged -= SentenceUdpRow_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (SentenceUdpPortItem row in e.NewItems.OfType<SentenceUdpPortItem>())
            {
                row.PropertyChanged += SentenceUdpRow_PropertyChanged;
            }
        }

        RefreshSentenceUdpFilter();
    }

    private void SentenceUdpRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SentenceUdpSearchText))
        {
            return;
        }

        if (e.PropertyName is nameof(SentenceUdpPortItem.UdpPort) or nameof(SentenceUdpPortItem.UdpAddress))
        {
            RefreshSentenceUdpFilter();
        }
    }
}
