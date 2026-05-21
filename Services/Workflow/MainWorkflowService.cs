using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;
using NMEASender.Wpf.ViewModels;

namespace NMEASender.Wpf.Services;

public sealed class MainWorkflowService : IMainWorkflowService
{
    private readonly DispatcherTimer _timer;
    private readonly IOutputChannelService _outputChannelService;
    private readonly IPortBaudRateService _portBaudRateService;
    private readonly INmeaTransmissionService _nmeaTransmissionService;
    private readonly IProjectSentenceFrameService _projectSentenceFrameService;
    private readonly ISharedMemoryProviderService _sharedMemoryDataProvider;
    private readonly ISentenceComposerService _sentenceComposer;
    private readonly ISentenceCatalogService _sentenceCatalog;
    private readonly ISerialPortCatalogService _serialPortCatalogService;
    private readonly IBaudRateSettingService _portBaudRateSettingsDialogService;
    private readonly IApplicationLifecycleService _applicationLifecycleService;
    private readonly IManualInputMapperService _manualInputMapperService;
    private readonly INmeaSenderConfigService _config;
    private bool _sharedMemoryWarningLogged;
    private bool _isSynchronizingAllComSentencesChecked;
    private bool _isSynchronizingAllUdpSentencesChecked;
    private NmeaDataDto _data = new();

    public MainWorkflowService(
        MainStateStore state,
        IOutputChannelService outputChannelService,
        IPortBaudRateService portBaudRateService,
        INmeaTransmissionService nmeaTransmissionService,
        IProjectSentenceFrameService projectSentenceFrameService,
        ISharedMemoryProviderService sharedMemoryDataProvider,
        ISentenceComposerService sentenceComposer,
        ISentenceCatalogService sentenceCatalog,
        ISerialPortCatalogService serialPortCatalogService,
        IBaudRateSettingService portBaudRateSettingsDialogService,
        IApplicationLifecycleService applicationLifecycleService,
        IManualInputMapperService manualInputMapperService,
        INmeaSenderConfigService config)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        _outputChannelService = outputChannelService ?? throw new ArgumentNullException(nameof(outputChannelService));
        _portBaudRateService = portBaudRateService ?? throw new ArgumentNullException(nameof(portBaudRateService));
        _nmeaTransmissionService = nmeaTransmissionService ?? throw new ArgumentNullException(nameof(nmeaTransmissionService));
        _projectSentenceFrameService = projectSentenceFrameService ?? throw new ArgumentNullException(nameof(projectSentenceFrameService));
        _sharedMemoryDataProvider = sharedMemoryDataProvider ?? throw new ArgumentNullException(nameof(sharedMemoryDataProvider));
        _sentenceComposer = sentenceComposer ?? throw new ArgumentNullException(nameof(sentenceComposer));
        _sentenceCatalog = sentenceCatalog ?? throw new ArgumentNullException(nameof(sentenceCatalog));
        _serialPortCatalogService = serialPortCatalogService ?? throw new ArgumentNullException(nameof(serialPortCatalogService));
        _portBaudRateSettingsDialogService = portBaudRateSettingsDialogService ?? throw new ArgumentNullException(nameof(portBaudRateSettingsDialogService));
        _applicationLifecycleService = applicationLifecycleService ?? throw new ArgumentNullException(nameof(applicationLifecycleService));
        _manualInputMapperService = manualInputMapperService ?? throw new ArgumentNullException(nameof(manualInputMapperService));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        State.Title = _config.Title;
        State.DefaultPort = _config.DefaultPort;
        State.UseTrueWind = _config.TrueWind;
        State.UseHdmOutput = _config.UseHdmOutput;
        State.UdpPortText = _config.UdpPort.ToString(CultureInfo.InvariantCulture);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_config.SendInterval) };
        _timer.Tick += (_, _) => SendTick();

        State.PropertyChanged += State_PropertyChanged;

        RefreshPorts();
        BuildSentenceRows();
        SetData();
        AddLog("COM Close");

        _ = StartAsync();
    }

    public MainStateStore State { get; }

    public IReadOnlyList<int> BaudRateOptions => _portBaudRateService.BaudRateOptions;

    public async Task StartAsync()
    {
        if (State.IsOpening || State.IsRunning)
        {
            return;
        }

        State.IsOpening = true;
        try
        {
            if (!UpdateCurrentData(forceLog: true))
            {
                return;
            }

            bool hasEnabledUdpSentence = HasEnabledUdpSentence();
            int udpPort = NormalizeUdpPort(_config.UdpPort);
            if (hasEnabledUdpSentence && !TryGetUdpPort(out udpPort, out string? udpPortError))
            {
                AddLog(udpPortError);
                return;
            }

            UdpTransportOptions udpTransportOptions = _config.UdpTransportOptions.WithFallbackPort(udpPort);
            bool useUdp = hasEnabledUdpSentence && udpTransportOptions.IsEnabled;
            if (hasEnabledUdpSentence && !udpTransportOptions.IsEnabled)
            {
                AddLog("UDP transport is disabled by profile.");
            }

            List<SentenceItem> comEnabledSentences = State.ConfigurableSentences()
                .Where(item => item.IsComEnabled)
                .ToList();

            if (comEnabledSentences.Count > 0)
            {
                RefreshPorts();
            }

            List<string> enabledPorts = comEnabledSentences
                .Select(item => item.PortName)
                .Where(port => !string.IsNullOrWhiteSpace(port))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (enabledPorts.Count == 0 && !useUdp)
            {
                AddLog("No COM port selected");
                return;
            }

            int udpOpenPort = udpTransportOptions.ResolveTargetPort(udpPort);

            TransmissionStartContext startContext = new(
                _config,
                enabledPorts,
                useUdp,
                udpOpenPort,
                udpTransportOptions,
                State.IsIosSource);

            TransmissionStartResult startResult = await _nmeaTransmissionService.StartAsync(startContext, AddLog);
            if (!startResult.Started)
            {
                ShowComOpenFailureDialog(comEnabledSentences, startResult.FailedComPorts);
                return;
            }

            ShowComOpenFailureDialog(comEnabledSentences, startResult.FailedComPorts);

            SaveConfig();
            State.IsRunning = true;
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
            State.IsOpening = false;
        }
    }

    public void Stop()
    {
        _timer.Stop();
        _nmeaTransmissionService.Stop(State.IsRunning, AddLog);
        State.IsRunning = false;
    }

    public void Exit()
    {
        SaveConfig();
        _applicationLifecycleService.RequestShutdown();
    }

    public void OpenSettings()
    {
        RefreshPorts();
        IReadOnlyDictionary<string, int> currentPortBaudRates = GetPortBaudRatesSnapshot();
        IReadOnlyList<SentenceUdpPortSetting> currentSentenceUdpPorts = GetSentenceUdpPortSettingsSnapshot();
        int udpFallbackPort = TryParseUdpPort(State.UdpPortText, out int parsedUdpPort)
            ? parsedUdpPort
            : NormalizeUdpPort(_config.UdpPort);
        UdpTransportOptions currentUdpTransportOptions = _config.UdpTransportOptions.WithFallbackPort(udpFallbackPort);

        if (!_portBaudRateSettingsDialogService.TryShow(
                currentPortBaudRates,
                BaudRateOptions,
                currentSentenceUdpPorts,
                currentUdpTransportOptions,
                _projectSentenceFrameService.SupportsPerSentenceMulticastAddress(_config.ProjectType),
                out IReadOnlyDictionary<string, int> updatedPortBaudRates,
                out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts,
                out IReadOnlyDictionary<string, string> updatedSentenceUdpAddresses,
                out UdpTransportOptions updatedUdpTransportOptions))
        {
            return;
        }

        if (!TryApplyPortBaudRates(updatedPortBaudRates, out string error))
        {
            AddLog($"Baud rate setting failed: {error}");
            return;
        }

        if (!TryNormalizeUdpTransportOptions(
                updatedUdpTransportOptions,
                out UdpTransportOptions normalizedUdpTransportOptions,
                out string udpTransportError))
        {
            AddLog($"UDP transport setting failed: {udpTransportError}");
            return;
        }

        if (!TryApplySentenceUdpSettings(
                updatedSentenceUdpPorts,
                updatedSentenceUdpAddresses,
                normalizedUdpTransportOptions.Mode,
                out string udpError))
        {
            AddLog($"Sentence UDP setting failed: {udpError}");
            return;
        }

        _config.UdpTransportOptions = normalizedUdpTransportOptions;
        SaveConfig();

        if (State.IsRunning)
        {
            _nmeaTransmissionService.HandleUdpToggleDuringRun(State.IsRunning, State.IsOpening, false, _config.UdpTransportOptions, AddLog);
            SyncUdpOutputStateDuringRun();
        }
    }

    public void SetData()
    {
        if (UpdateCurrentData(forceLog: true))
        {
            GeneratePreview();
        }
    }

    public void GetData()
    {
        if (State.IsIosSource)
        {
            UpdateCurrentData(forceLog: true);
        }

        ApplyManualInput(_manualInputMapperService.ToInputValues(_data));
        GeneratePreview();
    }

    public void ApplyDefaultPort()
    {
        foreach (SentenceItem item in State.AllSentences())
        {
            if (item.Id == NmeaSentenceId.STR)
            {
                continue;
            }

            item.PortName = State.DefaultPort;
        }

        SaveConfig();
    }

    public void ApplyDefaultUdpPort()
    {
        if (!TryGetUdpPort(out int udpPort, out string error))
        {
            AddLog(error);
            return;
        }

        foreach (SentenceItem item in State.ConfigurableSentences())
        {
            item.UdpPort = udpPort;
        }

        SaveConfig();
    }

    public void AddSentenceRow(SentenceItem? source)
    {
        if (!State.IsComSettingsEditable || source is null)
        {
            return;
        }

        if (TryDuplicateSentenceRow(State.GpsSentences, source) || TryDuplicateSentenceRow(State.OtherSentences, source))
        {
            GeneratePreview();
            SaveConfig();
        }
    }

    public void RemoveSentenceRow(SentenceItem? source)
    {
        if (!State.IsComSettingsEditable || source is not { IsDuplicateRow: true })
        {
            return;
        }

        if (TryRemoveSentenceRow(State.GpsSentences, source) || TryRemoveSentenceRow(State.OtherSentences, source))
        {
            SaveConfig();
        }
    }

    public void RefreshPorts()
    {
        string previousDefault = string.IsNullOrWhiteSpace(State.DefaultPort) ? _config.DefaultPort : State.DefaultPort;
        Dictionary<SentenceItem, string> previousSentencePorts = State.AllSentences().ToDictionary(item => item, item => item.PortName);
        IReadOnlyList<string> names = _serialPortCatalogService.GetSortedPorts(out string? portScanError);
        if (!string.IsNullOrWhiteSpace(portScanError))
        {
            AddLog($"COM scan failed: {portScanError}");
        }

        State.Ports.Clear();
        foreach (string port in names)
        {
            State.Ports.Add(port);
        }

        State.DefaultPort = _serialPortCatalogService.PickAvailablePort(State.Ports, previousDefault, _config.DefaultPort);
        foreach (var (item, portName) in previousSentencePorts)
        {
            item.PortName = _serialPortCatalogService.PickAvailablePort(State.Ports, portName, State.DefaultPort);
        }
    }

    public void ClearLog()
    {
        State.Logs.Clear();
    }

    public void Dispose()
    {
        State.PropertyChanged -= State_PropertyChanged;
        Stop();
    }

    private void SendTick()
    {
        if (!UpdateCurrentData())
        {
            return;
        }

        List<SentenceItem> enabledSentences = State.AllSentences()
            .Where(item => item.IsComEnabled || item.IsUdpEnabled)
            .ToList();
        int requestedUdpPort = TryParseUdpPort(State.UdpPortText, out int parsedUdpPort)
            ? parsedUdpPort
            : NormalizeUdpPort(_config.UdpPort);
        UdpTransportOptions udpTransportOptions = _config.UdpTransportOptions.WithFallbackPort(requestedUdpPort);
        int defaultUdpPort = udpTransportOptions.ResolveTargetPort(requestedUdpPort);

        TransmissionTickContext tickContext = new(
            enabledSentences,
            _data,
            State.IsIosSource,
            CurrentBuildOptions(),
            defaultUdpPort,
            udpTransportOptions);

        _nmeaTransmissionService.DispatchTick(tickContext, AddLog, Stop);
    }

    private void SyncUdpOutputStateDuringRun()
    {
        if (!State.IsRunning || State.IsOpening)
        {
            return;
        }

        bool hasEnabledUdpSentence = HasEnabledUdpSentence();
        int udpFallbackPort = TryParseUdpPort(State.UdpPortText, out int parsedUdpPort)
            ? parsedUdpPort
            : NormalizeUdpPort(_config.UdpPort);
        UdpTransportOptions udpTransportOptions = _config.UdpTransportOptions.WithFallbackPort(udpFallbackPort);
        bool shouldUseUdp = hasEnabledUdpSentence && udpTransportOptions.IsEnabled;

        if (hasEnabledUdpSentence && !udpTransportOptions.IsEnabled)
        {
            AddLog("UDP transport is disabled by profile.");
        }

        if (!shouldUseUdp)
        {
            _nmeaTransmissionService.HandleUdpToggleDuringRun(State.IsRunning, State.IsOpening, false, udpTransportOptions, AddLog);
            return;
        }

        _nmeaTransmissionService.HandleUdpToggleDuringRun(State.IsRunning, State.IsOpening, true, udpTransportOptions, AddLog);
    }

    private bool UpdateCurrentData(bool forceLog = false)
    {
        if (!State.IsIosSource)
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

    private void GeneratePreview()
    {
        NmeaBuildOptions options = CurrentBuildOptions();
        foreach (SentenceItem item in State.AllSentences())
        {
            _sentenceComposer.ComposeAndApplyPreview(item, _data, State.IsIosSource, options);
        }
    }

    private void BuildSentenceRows()
    {
        foreach (SentenceItem sentence in State.ConfigurableSentences())
        {
            sentence.PropertyChanged -= Sentence_PropertyChanged;
        }

        _sentenceCatalog.Populate(
            State.GpsSentences,
            State.OtherSentences,
            State.InternalSentences,
            _config,
            port => _serialPortCatalogService.PickAvailablePort(State.Ports, port, State.DefaultPort));

        foreach (SentenceItem sentence in State.ConfigurableSentences())
        {
            sentence.PropertyChanged += Sentence_PropertyChanged;
        }

        SynchronizeAllSentenceChecks();
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
            source.UdpAddress,
            source.HasSecondary,
            isDuplicateRow: true)
        {
            PrimaryText = source.PrimaryText,
            SecondaryText = source.SecondaryText
        };
    }

    private IEnumerable<(SentenceItem Item, string RowKey, int RowIndex)> EnumerateConfigurableSentenceRows()
    {
        Dictionary<NmeaSentenceId, int> rowIndexBySentence = new();
        foreach (SentenceItem item in State.ConfigurableSentences())
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

    private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(MainStateStore.IsTestSource):
                if (State.IsTestSource && State.IsIosSource)
                {
                    State.IsIosSource = false;
                }
                break;

            case nameof(MainStateStore.IsIosSource):
                if (State.IsIosSource && State.IsTestSource)
                {
                    State.IsTestSource = false;
                }
                break;

            case nameof(MainStateStore.UseTrueWind):
                GeneratePreview();
                break;

            case nameof(MainStateStore.DefaultPort):
                string trimmedDefaultPort = (State.DefaultPort ?? string.Empty).Trim();
                if (!string.Equals(trimmedDefaultPort, State.DefaultPort, StringComparison.Ordinal))
                {
                    State.DefaultPort = trimmedDefaultPort;
                }
                break;

            case nameof(MainStateStore.UdpPortText):
                string trimmedUdpPortText = (State.UdpPortText ?? string.Empty).Trim();
                if (!string.Equals(trimmedUdpPortText, State.UdpPortText, StringComparison.Ordinal))
                {
                    State.UdpPortText = trimmedUdpPortText;
                }
                break;

            case nameof(MainStateStore.AreAllComSentencesChecked):
                if (_isSynchronizingAllComSentencesChecked)
                {
                    return;
                }
                ApplyAllComSelection(State.AreAllComSentencesChecked);
                break;

            case nameof(MainStateStore.AreAllUdpSentencesChecked):
                if (_isSynchronizingAllUdpSentencesChecked)
                {
                    return;
                }
                ApplyAllUdpSelection(State.AreAllUdpSentencesChecked);
                break;
        }
    }

    private void Sentence_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SentenceItem.IsComEnabled) &&
            e.PropertyName != nameof(SentenceItem.IsUdpEnabled))
        {
            return;
        }

        if (e.PropertyName == nameof(SentenceItem.IsComEnabled) && sender is SentenceItem sentence)
        {
            HandleComToggleDuringRun(sentence);
        }

        if (e.PropertyName == nameof(SentenceItem.IsComEnabled) && _isSynchronizingAllComSentencesChecked)
        {
            return;
        }

        if (e.PropertyName == nameof(SentenceItem.IsUdpEnabled) && _isSynchronizingAllUdpSentencesChecked)
        {
            return;
        }

        if (e.PropertyName == nameof(SentenceItem.IsUdpEnabled))
        {
            SyncUdpOutputStateDuringRun();
        }

        SynchronizeAllSentenceChecks();
    }

    private void HandleComToggleDuringRun(SentenceItem sentence)
    {
        if (!State.IsRunning || State.IsOpening || !sentence.IsComEnabled)
        {
            return;
        }

        string portName = (sentence.PortName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(portName))
        {
            AddLog($"{sentence.Label} COM not selected");
            return;
        }

        if (_outputChannelService.IsComPortOpen(portName))
        {
            return;
        }

        if (_outputChannelService.TryOpenCom(
                portName,
                _config.BaudRate,
                _config.PortBaudRates,
                _config.DataBits,
                _config.Parity,
                _config.StopBits,
                out string? error))
        {
            int baudRate = _portBaudRateService.ResolveBaudRate(_config, portName);
            AddLog($"{portName} Open Success ({baudRate} bps)");
            return;
        }

        AddLog($"{portName} Open Fail: {error}");
    }

    private void ApplyAllComSelection(bool isChecked)
    {
        _isSynchronizingAllComSentencesChecked = true;
        try
        {
            foreach (SentenceItem item in State.ConfigurableSentences())
            {
                item.IsComEnabled = isChecked;
            }
        }
        finally
        {
            _isSynchronizingAllComSentencesChecked = false;
        }

        SynchronizeAllSentenceChecks();
    }

    private void ApplyAllUdpSelection(bool isChecked)
    {
        _isSynchronizingAllUdpSentencesChecked = true;
        try
        {
            foreach (SentenceItem item in State.ConfigurableSentences())
            {
                item.IsUdpEnabled = isChecked;
            }
        }
        finally
        {
            _isSynchronizingAllUdpSentencesChecked = false;
        }

        SyncUdpOutputStateDuringRun();
        SynchronizeAllSentenceChecks();
    }

    private void SynchronizeAllSentenceChecks()
    {
        bool isAllComChecked = State.ConfigurableSentences().All(item => item.IsComEnabled);
        bool isAllUdpChecked = State.ConfigurableSentences().All(item => item.IsUdpEnabled);

        _isSynchronizingAllComSentencesChecked = true;
        _isSynchronizingAllUdpSentencesChecked = true;
        try
        {
            State.AreAllComSentencesChecked = isAllComChecked;
            State.AreAllUdpSentencesChecked = isAllUdpChecked;
        }
        finally
        {
            _isSynchronizingAllComSentencesChecked = false;
            _isSynchronizingAllUdpSentencesChecked = false;
        }
    }

    private IReadOnlyDictionary<string, int> GetPortBaudRatesSnapshot()
    {
        IEnumerable<string> sentencePorts = State.AllSentences().Select(item => item.PortName);
        return _portBaudRateService.CreateSnapshot(_config, State.Ports, sentencePorts, State.DefaultPort);
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
                NormalizeUdpPort(item.UdpPort),
                ResolveSentenceUdpAddress(item.UdpAddress, _config.UdpTransportOptions.MulticastAddress)));
        }

        return settings;
    }

    private bool TryApplyPortBaudRates(IReadOnlyDictionary<string, int> portBaudRates, out string error)
    {
        if (!_portBaudRateService.TryApply(_config, portBaudRates, out error))
        {
            return false;
        }

        if (State.IsRunning)
        {
            AddLog("Baud rate settings saved. Restart START to apply.");
        }

        return true;
    }

    private bool TryApplySentenceUdpSettings(
        IReadOnlyDictionary<string, int> sentenceUdpPorts,
        IReadOnlyDictionary<string, string> sentenceUdpAddresses,
        UdpTransportMode udpMode,
        out string error)
    {
        error = string.Empty;
        Dictionary<string, int> normalizedUdpPorts = new(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, string> normalizedUdpAddresses = new(StringComparer.OrdinalIgnoreCase);
        bool requireMulticastAddressValidation =
            _projectSentenceFrameService.SupportsPerSentenceMulticastAddress(_config.ProjectType) &&
            udpMode == UdpTransportMode.Multicast;

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

        foreach ((string rowKey, string rawAddress) in sentenceUdpAddresses)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                continue;
            }

            string candidateAddress = (rawAddress ?? string.Empty).Trim();
            if (requireMulticastAddressValidation)
            {
                if (!TryNormalizeMulticastAddress(candidateAddress, out string normalizedAddress))
                {
                    error = $"{rowKey} multicast address must be in 224.0.0.0 - 239.255.255.255.";
                    return false;
                }

                normalizedUdpAddresses[rowKey] = normalizedAddress;
                continue;
            }

            normalizedUdpAddresses[rowKey] = UdpTransportOptions.NormalizeAddress(
                candidateAddress,
                _config.UdpTransportOptions.MulticastAddress);
        }

        foreach ((SentenceItem item, string rowKey, _) in EnumerateConfigurableSentenceRows())
        {
            if (!normalizedUdpPorts.TryGetValue(rowKey, out int udpPort))
            {
                continue;
            }

            item.UdpPort = udpPort;
            if (normalizedUdpAddresses.TryGetValue(rowKey, out string? udpAddress) &&
                !string.IsNullOrWhiteSpace(udpAddress))
            {
                item.UdpAddress = udpAddress;
            }
        }

        return true;
    }

    private bool TryNormalizeUdpTransportOptions(
        UdpTransportOptions udpTransportOptions,
        out UdpTransportOptions normalizedUdpTransportOptions,
        out string error)
    {
        error = string.Empty;
        normalizedUdpTransportOptions = _config.UdpTransportOptions;
        UdpTransportMode mode = udpTransportOptions.Mode == UdpTransportMode.Multicast
            ? UdpTransportMode.Multicast
            : UdpTransportMode.Broadcast;
        string multicastAddress = UdpTransportOptions.NormalizeAddress(udpTransportOptions.MulticastAddress, "225.0.0.0");

        if (mode == UdpTransportMode.Multicast)
        {
            if (!IPAddress.TryParse(multicastAddress, out IPAddress? address) ||
                address.AddressFamily != AddressFamily.InterNetwork)
            {
                error = "Multicast address must be a valid IPv4 address.";
                return false;
            }

            if (!IsMulticastAddress(address))
            {
                error = "Multicast address must be in 224.0.0.0 - 239.255.255.255.";
                return false;
            }
        }

        int fallbackUdpPort = TryParseUdpPort(State.UdpPortText, out int parsedUdpPort)
            ? parsedUdpPort
            : NormalizeUdpPort(_config.UdpPort);

        normalizedUdpTransportOptions = (udpTransportOptions with
        {
            Mode = mode,
            MulticastAddress = multicastAddress
        }).WithFallbackPort(fallbackUdpPort);
        return true;
    }

    private void SaveConfig()
    {
        try
        {
            _config.Title = State.Title;
            _config.DefaultPort = State.DefaultPort;
            _config.TrueWind = State.UseTrueWind;
            _config.UseHdmOutput = State.UseHdmOutput;
            _config.UseUdp = HasEnabledUdpSentence();
            if (TryGetUdpPort(out int udpPort, out _))
            {
                _config.UdpPort = udpPort;
            }

            _config.Save(State.AllSentences());
        }
        catch (Exception ex)
        {
            AddLog($"Config save failed: {ex.Message}");
        }
    }

    private void AddLog(string message)
    {
        if (State.Logs.Count > 1000)
        {
            State.Logs.RemoveAt(0);
        }

        State.Logs.Add(message);
    }

    private NmeaBuildOptions CurrentBuildOptions()
    {
        return new NmeaBuildOptions(State.UseTrueWind, State.UseHdmOutput, _config.ProjectType);
    }

    private ManualInputValues CurrentManualInput()
    {
        return new ManualInputValues(State.LongitudeText, State.LatitudeText, State.SpeedText, State.HeadingText);
    }

    private void ApplyManualInput(ManualInputValues values)
    {
        State.LongitudeText = values.Longitude;
        State.LatitudeText = values.Latitude;
        State.SpeedText = values.Speed;
        State.HeadingText = values.Heading;
    }

    private bool HasEnabledUdpSentence()
    {
        return State.ConfigurableSentences().Any(item => item.IsUdpEnabled);
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
        if (TryParseUdpPort(State.UdpPortText, out port))
        {
            error = string.Empty;
            return true;
        }

        error = "UDP port must be between 1 and 65535.";
        return false;
    }

    private static bool IsMulticastAddress(IPAddress address)
    {
        byte[] bytes = address.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] is >= 224 and <= 239;
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

    private static string ResolveSentenceUdpAddress(string? value, string fallbackAddress)
    {
        string candidate = (value ?? string.Empty).Trim();
        return UdpTransportOptions.NormalizeAddress(candidate, fallbackAddress);
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
}
