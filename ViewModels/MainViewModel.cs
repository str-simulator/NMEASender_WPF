using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly IOutputChannelService _outputChannelService;
    private readonly IPortBaudRateService _portBaudRateService;
    private readonly INmeaTransmissionService _nmeaTransmissionService;
    private readonly ISharedMemoryProviderService _sharedMemoryDataProvider;
    private readonly ISentenceComposerService _sentenceComposer;
    private readonly ISentenceCatalogService _sentenceCatalog;
    private readonly ISerialPortCatalogService _serialPortCatalogService;
    private readonly IBaudRateSettingService _portBaudRateSettingsDialogService;
    private readonly IApplicationLifecycleService _applicationLifecycleService;
    private readonly IManualInputMapperService _manualInputMapperService;
    private readonly INmeaSenderConfigService _config;
    private bool _sharedMemoryWarningLogged;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComSettingsEditable))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSentenceRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveSentenceRowCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsComSettingsEditable))]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyCanExecuteChangedFor(nameof(AddSentenceRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(RemoveSentenceRowCommand))]
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
    private bool _useUdp;

    [ObservableProperty]
    private bool _areAllComSentencesChecked = true;

    [ObservableProperty]
    private bool _areAllUdpSentencesChecked = true;
    private bool _isSynchronizingAllComSentencesChecked;
    private bool _isSynchronizingAllUdpSentencesChecked;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _defaultPort = string.Empty;

    [ObservableProperty]
    private string _udpPortText = "40014";

    [ObservableProperty]
    private string _longitudeText = "129.0000";

    [ObservableProperty]
    private string _latitudeText = "35.0000";

    [ObservableProperty]
    private string _speedText = "0.0";

    [ObservableProperty]
    private string _headingText = "0.0";

    private NmeaDataDto _data = new();

    public MainViewModel(
        IOutputChannelService outputChannelService,
        IPortBaudRateService portBaudRateService,
        INmeaTransmissionService nmeaTransmissionService,
        ISharedMemoryProviderService sharedMemoryDataProvider,
        ISentenceComposerService sentenceComposer,
        ISentenceCatalogService sentenceCatalog,
        ISerialPortCatalogService serialPortCatalogService,
        IBaudRateSettingService portBaudRateSettingsDialogService,
        IApplicationLifecycleService applicationLifecycleService,
        IManualInputMapperService manualInputMapperService,
        INmeaSenderConfigService config)
    {
        _outputChannelService = outputChannelService ?? throw new ArgumentNullException(nameof(outputChannelService));
        _portBaudRateService = portBaudRateService ?? throw new ArgumentNullException(nameof(portBaudRateService));
        _nmeaTransmissionService = nmeaTransmissionService ?? throw new ArgumentNullException(nameof(nmeaTransmissionService));
        _sharedMemoryDataProvider = sharedMemoryDataProvider ?? throw new ArgumentNullException(nameof(sharedMemoryDataProvider));
        _sentenceComposer = sentenceComposer ?? throw new ArgumentNullException(nameof(sentenceComposer));
        _sentenceCatalog = sentenceCatalog ?? throw new ArgumentNullException(nameof(sentenceCatalog));
        _serialPortCatalogService = serialPortCatalogService ?? throw new ArgumentNullException(nameof(serialPortCatalogService));
        _portBaudRateSettingsDialogService = portBaudRateSettingsDialogService ?? throw new ArgumentNullException(nameof(portBaudRateSettingsDialogService));
        _applicationLifecycleService = applicationLifecycleService ?? throw new ArgumentNullException(nameof(applicationLifecycleService));
        _manualInputMapperService = manualInputMapperService ?? throw new ArgumentNullException(nameof(manualInputMapperService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _title = _config.Title;
        _defaultPort = _config.DefaultPort;
        _useTrueWind = _config.TrueWind;
        _useHdmOutput = _config.UseHdmOutput;
        _useUdp = _config.UseUdp;
        _udpPortText = _config.UdpPort.ToString(CultureInfo.InvariantCulture);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_config.SendInterval) };
        _timer.Tick += (_, _) => SendTick();

        RefreshPorts();
        BuildSentenceRows();
        SetData();
        AddLog("COM Close");

        _ = Start();
    }

    public ObservableCollection<string> Ports { get; } = new();

    public ObservableCollection<string> Logs { get; } = new();

    public ObservableCollection<SentenceItem> GpsSentences { get; } = new();

    public ObservableCollection<SentenceItem> OtherSentences { get; } = new();

    private readonly List<SentenceItem> _internalSentences = new();

    public bool IsComSettingsEditable => !IsRunning && !IsOpening;

    public IReadOnlyList<int> BaudRateOptions => _portBaudRateService.BaudRateOptions;

    private IReadOnlyDictionary<string, int> GetPortBaudRatesSnapshot()
    {
        IEnumerable<string> sentencePorts = AllSentences().Select(item => item.PortName);
        return _portBaudRateService.CreateSnapshot(_config, Ports, sentencePorts, DefaultPort);
    }

    private IReadOnlyList<SentenceUdpPortSetting> GetSentenceUdpPortSettingsSnapshot()
    {
        List<SentenceUdpPortSetting> settings = new();
        foreach ((SentenceItem item, string rowKey, int rowIndex) in EnumerateConfigurableSentenceRows())
        {
            string displayLabel = rowIndex > 1 ? $"{item.Label} #{rowIndex}" : item.Label;
            settings.Add(new SentenceUdpPortSetting(
                rowKey,
                displayLabel,
                NormalizeUdpPort(item.UdpPort)));
        }

        return settings;
    }

    private bool TryApplyPortBaudRates(IReadOnlyDictionary<string, int> portBaudRates, out string error)
    {
        if (!_portBaudRateService.TryApply(_config, portBaudRates, out error))
        {
            return false;
        }

        if (IsRunning)
        {
            AddLog("Baud rate settings saved. Restart START to apply.");
        }

        return true;
    }

    private bool TryApplySentenceUdpPorts(IReadOnlyDictionary<string, int> sentenceUdpPorts, out string error)
    {
        error = string.Empty;
        Dictionary<string, int> normalizedUdpPorts = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string rowKey, int udpPort) in sentenceUdpPorts)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                continue;
            }

            if (udpPort is < 1 or > 65535)
            {
                error = $"{rowKey} UDP port must be between 1 and 65535.";
                return false;
            }

            normalizedUdpPorts[rowKey] = udpPort;
        }

        foreach ((SentenceItem item, string rowKey, _) in EnumerateConfigurableSentenceRows())
        {
            if (!normalizedUdpPorts.TryGetValue(rowKey, out int udpPort))
            {
                continue;
            }

            item.UdpPort = udpPort;
        }

        return true;
    }

    partial void OnIsTestSourceChanged(bool value)
    {
        if (value && IsIosSource)
        {
            IsIosSource = false;
        }
    }

    partial void OnAreAllComSentencesCheckedChanged(bool value)
    {
        if (_isSynchronizingAllComSentencesChecked)
        {
            return;
        }

        foreach (SentenceItem item in ConfigurableSentences())
        {
            item.IsComEnabled = value;
        }
    }

    partial void OnAreAllUdpSentencesCheckedChanged(bool value)
    {
        if (_isSynchronizingAllUdpSentencesChecked)
        {
            return;
        }

        foreach (SentenceItem item in ConfigurableSentences())
        {
            item.IsUdpEnabled = value;
        }
    }

    partial void OnIsIosSourceChanged(bool value)
    {
        if (value && IsTestSource)
        {
            IsTestSource = false;
        }
    }

    partial void OnUseTrueWindChanged(bool value)
    {
        GeneratePreview();
    }

    partial void OnUseUdpChanged(bool value)
    {
        GeneratePreview();
        HandleUdpToggleDuringRun();
    }

    partial void OnDefaultPortChanged(string value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        if (!string.Equals(trimmed, value, StringComparison.Ordinal))
        {
            DefaultPort = trimmed;
        }
    }

    partial void OnUdpPortTextChanged(string value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        if (!string.Equals(trimmed, value, StringComparison.Ordinal))
        {
            UdpPortText = trimmed;
        }
    }

    public void Dispose()
    {
        Stop();
        _outputChannelService.Dispose();
        _sharedMemoryDataProvider.Dispose();
    }

    private bool CanStart()
    {
        return !IsRunning && !IsOpening;
    }

    [RelayCommand(CanExecute = nameof(CanStart))]
    private async Task Start()
    {
        if (IsOpening || IsRunning)
        {
            return;
        }

        IsOpening = true;
        try
        {
            if (!UpdateCurrentData(forceLog: true))
            {
                return;
            }

            int udpPort = 0;
            if (UseUdp && !TryGetUdpPort(out udpPort, out string? udpPortError))
            {
                AddLog(udpPortError);
                return;
            }

            RefreshPorts();

            List<SentenceItem> comEnabledSentences = ConfigurableSentences()
                .Where(item => item.IsComEnabled)
                .ToList();

            List<string> enabledPorts = comEnabledSentences
                .Where(item => item.IsComEnabled)
                .Select(item => item.PortName)
                .Where(port => !string.IsNullOrWhiteSpace(port))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (enabledPorts.Count == 0 && !UseUdp)
            {
                AddLog("No COM port selected");
                return;
            }

            TransmissionStartContext startContext = new(
                _config,
                enabledPorts,
                UseUdp,
                udpPort,
                IsIosSource);

            TransmissionStartResult startResult = await _nmeaTransmissionService.StartAsync(startContext, AddLog);
            if (!startResult.Started)
            {
                ShowComOpenFailureDialog(comEnabledSentences, startResult.FailedComPorts);
                return;
            }

            ShowComOpenFailureDialog(comEnabledSentences, startResult.FailedComPorts);

            SaveConfig();
            IsRunning = true;
            _timer.Start();
            SendTick();
        }
        catch (Exception ex)
        {
            AddLog($"START failed: {ex.Message}");
            Stop();
        }
        finally
        {
            IsOpening = false;
        }
    }

    private bool CanStop()
    {
        return IsRunning;
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        _timer.Stop();
        _nmeaTransmissionService.Stop(IsRunning, AddLog);
        IsRunning = false;
    }

    [RelayCommand]
    private void Exit()
    {
        SaveConfig();
        _applicationLifecycleService.RequestShutdown();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        RefreshPorts();
        IReadOnlyDictionary<string, int> currentPortBaudRates = GetPortBaudRatesSnapshot();
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts = GetSentenceUdpPortSettingsSnapshot();

        if (!_portBaudRateSettingsDialogService.TryShow(
                currentPortBaudRates,
                BaudRateOptions,
                currentSentenceUdpPorts,
                out IReadOnlyDictionary<string, int> updatedPortBaudRates,
                out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts))
        {
            return;
        }

        if (!TryApplyPortBaudRates(updatedPortBaudRates, out string error))
        {
            AddLog($"Baud rate setting failed: {error}");
            return;
        }

        if (!TryApplySentenceUdpPorts(updatedSentenceUdpPorts, out string udpError))
        {
            AddLog($"Sentence UDP port setting failed: {udpError}");
            return;
        }

        SaveConfig();
    }

    private void SendTick()
    {
        if (!UpdateCurrentData())
        {
            return;
        }

        List<SentenceItem> enabledSentences = AllSentences()
            .Where(item => item.IsComEnabled || item.IsUdpEnabled)
            .ToList();
        int defaultUdpPort = TryParseUdpPort(UdpPortText, out int parsedUdpPort)
            ? parsedUdpPort
            : NormalizeUdpPort(_config.UdpPort);
        TransmissionTickContext tickContext = new(
            enabledSentences,
            _data,
            IsIosSource,
            CurrentBuildOptions(),
            defaultUdpPort);

        _nmeaTransmissionService.DispatchTick(tickContext, AddLog, Stop);
    }

    private void HandleUdpToggleDuringRun()
    {
        if (!TryGetUdpPort(out int udpPort, out string? udpPortError))
        {
            if (UseUdp)
            {
                AddLog(udpPortError);
            }
            return;
        }

        _nmeaTransmissionService.HandleUdpToggleDuringRun(IsRunning, IsOpening, UseUdp, udpPort, AddLog);
    }

    private bool UpdateCurrentData(bool forceLog = false)
    {
        if (!IsIosSource)
        {
            _data = _manualInputMapperService.ApplyToData(_data, CurrentManualInput());
            _sharedMemoryWarningLogged = false;
            return true;
        }

        if (_sharedMemoryDataProvider.TryRead(out NmeaDataDto? data, out string? error))
        {
            _data = data;
            _sharedMemoryWarningLogged = false;
            ApplyManualInput(_manualInputMapperService.ToInputValues(_data));
            return true;
        }

        if (forceLog || !_sharedMemoryWarningLogged)
        {
            AddLog($"SharedMemory Read Fail: {error}");
            _sharedMemoryWarningLogged = true;
        }

        return false;
    }

    [RelayCommand]
    private void SetData()
    {
        if (UpdateCurrentData(forceLog: true))
        {
            GeneratePreview();
        }
    }

    [RelayCommand]
    private void GetData()
    {
        if (IsIosSource)
        {
            UpdateCurrentData(forceLog: true);
        }

        ApplyManualInput(_manualInputMapperService.ToInputValues(_data));
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        NmeaBuildOptions options = CurrentBuildOptions();
        foreach (SentenceItem item in AllSentences())
        {
            _sentenceComposer.ComposeAndApplyPreview(item, _data, IsIosSource, options);
        }
    }

    [RelayCommand]
    private void ApplyDefaultPort()
    {
        foreach (SentenceItem item in AllSentences())
        {
            if (item.Id == NmeaSentenceId.STR)
            {
                continue;
            }

            item.PortName = DefaultPort;
        }

        SaveConfig();
    }

    private bool CanAddSentenceRow(SentenceItem? source)
    {
        return IsComSettingsEditable && source is not null;
    }

    [RelayCommand(CanExecute = nameof(CanAddSentenceRow))]
    private void AddSentenceRow(SentenceItem? source)
    {
        if (source is null)
        {
            return;
        }

        if (TryDuplicateSentenceRow(GpsSentences, source) || TryDuplicateSentenceRow(OtherSentences, source))
        {
            GeneratePreview();
            SaveConfig();
        }
    }

    private bool CanRemoveSentenceRow(SentenceItem? parameter)
    {
        return IsComSettingsEditable && parameter is { IsDuplicateRow: true };
    }

    [RelayCommand(CanExecute = nameof(CanRemoveSentenceRow))]
    private void RemoveSentenceRow(SentenceItem? source)
    {
        if (source is not { IsDuplicateRow: true })
        {
            return;
        }

        if (TryRemoveSentenceRow(GpsSentences, source) || TryRemoveSentenceRow(OtherSentences, source))
        {
            SaveConfig();
        }
    }

    private bool TryDuplicateSentenceRow(ObservableCollection<SentenceItem> collection, SentenceItem source)
    {
        int index = collection.IndexOf(source);
        if (index < 0)
        {
            return false;
        }

        SentenceItem duplicate = CloneSentenceItem(source);
        duplicate.PropertyChanged += Sentence_PropertyChanged;
        collection.Insert(index + 1, duplicate);
        SynchronizeAllSentenceChecks();
        return true;
    }

    private bool TryRemoveSentenceRow(ObservableCollection<SentenceItem> collection, SentenceItem source)
    {
        int index = collection.IndexOf(source);
        if (index < 0)
        {
            return false;
        }

        source.PropertyChanged -= Sentence_PropertyChanged;
        collection.RemoveAt(index);
        SynchronizeAllSentenceChecks();
        return true;
    }

    private static SentenceItem CloneSentenceItem(SentenceItem source)
    {
        return new SentenceItem(
            source.Id,
            source.Flag,
            source.Label,
            source.PortName,
            source.IsComEnabled,
            source.IsUdpEnabled,
            source.UdpPort,
            source.HasSecondary,
            isDuplicateRow: true)
        {
            PrimaryText = source.PrimaryText,
            SecondaryText = source.SecondaryText
        };
    }

    [RelayCommand]
    private void RefreshPorts()
    {
        string previousDefault = string.IsNullOrWhiteSpace(DefaultPort) ? _config.DefaultPort : DefaultPort;
        Dictionary<SentenceItem, string> previousSentencePorts = AllSentences().ToDictionary(item => item, item => item.PortName);
        IReadOnlyList<string> names = _serialPortCatalogService.GetSortedPorts(out string? portScanError);
        if (!string.IsNullOrWhiteSpace(portScanError))
        {
            AddLog($"COM scan failed: {portScanError}");
        }

        Ports.Clear();
        foreach (string port in names)
        {
            Ports.Add(port);
        }

        DefaultPort = _serialPortCatalogService.PickAvailablePort(Ports, previousDefault, _config.DefaultPort);
        foreach (var (item, portName) in previousSentencePorts)
        {
            item.PortName = _serialPortCatalogService.PickAvailablePort(Ports, portName, DefaultPort);
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        Logs.Clear();
    }

    private void BuildSentenceRows()
    {
        foreach (SentenceItem sentence in ConfigurableSentences())
        {
            sentence.PropertyChanged -= Sentence_PropertyChanged;
        }

        _sentenceCatalog.Populate(
            GpsSentences,
            OtherSentences,
            _internalSentences,
            _config,
            port => _serialPortCatalogService.PickAvailablePort(Ports, port, DefaultPort));

        foreach (SentenceItem sentence in ConfigurableSentences())
        {
            sentence.PropertyChanged += Sentence_PropertyChanged;
        }

        SynchronizeAllSentenceChecks();
    }

    private IEnumerable<SentenceItem> AllSentences()
    {
        return GpsSentences.Concat(OtherSentences).Concat(_internalSentences);
    }

    private IEnumerable<SentenceItem> ConfigurableSentences()
    {
        return GpsSentences.Concat(OtherSentences);
    }

    private IEnumerable<(SentenceItem Item, string RowKey, int RowIndex)> EnumerateConfigurableSentenceRows()
    {
        Dictionary<NmeaSentenceId, int> rowIndexBySentence = new();
        foreach (SentenceItem item in ConfigurableSentences())
        {
            int rowIndex = rowIndexBySentence.TryGetValue(item.Id, out int currentIndex)
                ? currentIndex + 1
                : 1;

            rowIndexBySentence[item.Id] = rowIndex;
            yield return (item, BuildSentenceRowKey(item.Id, rowIndex), rowIndex);
        }
    }

    private static string BuildSentenceRowKey(NmeaSentenceId id, int rowIndex)
    {
        string key = id.ToString().ToUpperInvariant();
        return rowIndex <= 1 ? key : $"{key}#{rowIndex}";
    }

    private void Sentence_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SentenceItem.IsComEnabled) &&
            e.PropertyName != nameof(SentenceItem.IsUdpEnabled))
        {
            return;
        }

        SynchronizeAllSentenceChecks();
    }

    private void SynchronizeAllSentenceChecks()
    {
        bool isAllComChecked = ConfigurableSentences().All(item => item.IsComEnabled);
        bool isAllUdpChecked = ConfigurableSentences().All(item => item.IsUdpEnabled);

        _isSynchronizingAllComSentencesChecked = true;
        _isSynchronizingAllUdpSentencesChecked = true;
        try
        {
            AreAllComSentencesChecked = isAllComChecked;
            AreAllUdpSentencesChecked = isAllUdpChecked;
        }
        finally
        {
            _isSynchronizingAllComSentencesChecked = false;
            _isSynchronizingAllUdpSentencesChecked = false;
        }
    }

    private void SaveConfig()
    {
        try
        {
            _config.Title = Title;
            _config.DefaultPort = DefaultPort;
            _config.TrueWind = UseTrueWind;
            _config.UseHdmOutput = UseHdmOutput;
            _config.UseUdp = UseUdp;
            if (TryGetUdpPort(out int udpPort, out _))
            {
                _config.UdpPort = udpPort;
            }

            _config.Save(AllSentences());
        }
        catch (Exception ex)
        {
            AddLog($"Config save failed: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        if (Logs.Count > 1000)
        {
            Logs.RemoveAt(0);
        }

        Logs.Add(message);
    }

    private static void ShowComOpenFailureDialog(
        IReadOnlyList<SentenceItem> comEnabledSentences,
        IReadOnlyList<PortOpenOutcome> failedComPorts)
    {
        if (failedComPorts.Count == 0 || comEnabledSentences.Count == 0)
        {
            return;
        }

        HashSet<string> failedPortSet = failedComPorts
            .Select(result => result.PortName)
            .Where(port => !string.IsNullOrWhiteSpace(port))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<string> failedSentenceLines = comEnabledSentences
            .Where(item => failedPortSet.Contains(item.PortName))
            .Select(item => $"{item.Label} ({item.PortName})")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        string body = failedSentenceLines.Count > 0
            ? string.Join(Environment.NewLine, failedSentenceLines)
            : string.Join(Environment.NewLine, failedComPorts.Select(result => result.PortName));

        MessageBox.Show(
            $"COM Open Failed.{Environment.NewLine}{Environment.NewLine}{body}",
            "COM Open Failed",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private NmeaBuildOptions CurrentBuildOptions()
    {
        return new NmeaBuildOptions(UseTrueWind, UseHdmOutput, _config.ProjectType);
    }

    private ManualInputValues CurrentManualInput()
    {
        return new ManualInputValues(LongitudeText, LatitudeText, SpeedText, HeadingText);
    }

    private void ApplyManualInput(ManualInputValues values)
    {
        LongitudeText = values.Longitude;
        LatitudeText = values.Latitude;
        SpeedText = values.Speed;
        HeadingText = values.Heading;
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

    private bool TryGetUdpPort(out int port, out string error)
    {
        if (TryParseUdpPort(UdpPortText, out port))
        {
            error = string.Empty;
            return true;
        }

        error = "UDP port must be between 1 and 65535.";
        return false;
    }

}
