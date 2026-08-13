using NMEASender.Wpf.Exceptions;
using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Application;
using NMEASender.Wpf.Services.Interfaces.Config;
using NMEASender.Wpf.Services.Interfaces.IO;
using NMEASender.Wpf.Services.Interfaces.Mapping;
using NMEASender.Wpf.Services.Interfaces.Ports;
using NMEASender.Wpf.Services.Interfaces.Transmission;
using NMEASender.Wpf.Services.Interfaces.Workflow;
using NMEASender.Wpf.Services.Mapping;
using NMEASender.Wpf.ViewModels.Shell;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Threading;

namespace NMEASender.Wpf.Services.Workflow;

public sealed class MainWorkflowService : IMainWorkflowService
{
    private readonly Timer _sendTimer;
    private readonly DispatcherTimer _sharedMemoryRetryTimer;
    private readonly IOutputChannelService _outputChannelService;
    private readonly IPortBaudRateService _portBaudRateService;
    private readonly INmeaTransmissionService _nmeaTransmissionService;
    private readonly IProjectSentenceFrameService _projectSentenceFrameService;
    private readonly ISharedMemoryProviderService _sharedMemoryDataProvider;
    private readonly ISentenceComposerService _sentenceComposer;
    private readonly ISentenceCatalogService _sentenceCatalog;
    private readonly ISerialPortCatalogService _serialPortCatalogService;
    private readonly IBaudRateSettingService _portBaudRateSettingsDialogService;
    private readonly ITransmissionSummaryDialogService _transmissionSummaryDialogService;
    private readonly IApplicationLifecycleService _applicationLifecycleService;
    private readonly IManualInputMapperService _manualInputMapperService;
    private readonly INmeaSenderConfigService _config;
    private bool _sharedMemoryWarningLogged;
    private bool _isSynchronizingAllComSentencesChecked;
    private bool _isSynchronizingAllUdpSentencesChecked;
    private int _isSending;
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
        ITransmissionSummaryDialogService transmissionSummaryDialogService,
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
        _transmissionSummaryDialogService = transmissionSummaryDialogService ?? throw new ArgumentNullException(nameof(transmissionSummaryDialogService));
        _applicationLifecycleService = applicationLifecycleService ?? throw new ArgumentNullException(nameof(applicationLifecycleService));
        _manualInputMapperService = manualInputMapperService ?? throw new ArgumentNullException(nameof(manualInputMapperService));
        _config = config ?? throw new ArgumentNullException(nameof(config));

        State.Title = _config.Title;
        State.DefaultPort = _config.DefaultPort;
        State.UseTrueWind = _config.TrueWind;
        State.UseHdmOutput = _config.UseHdmOutput;
        State.UdpPortText = _config.UdpPort.ToString(CultureInfo.InvariantCulture);

        // Fires on a thread-pool thread so the tick itself is never delayed by
        // Dispatcher queue congestion (e.g. UI rendering under heavy CPU load);
        // the actual send work is then marshaled to the UI thread at Send priority.
        _sendTimer = new Timer(_ =>
        {
            if (State.IsRunning)
            {
                System.Windows.Application.Current?.Dispatcher.BeginInvoke(SendTick, DispatcherPriority.Send);
            }
        }, null, Timeout.Infinite, Timeout.Infinite);

        _sharedMemoryRetryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _sharedMemoryRetryTimer.Tick += OnSharedMemoryRetryTick;

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
                if (State.IsIosSource && !_sharedMemoryRetryTimer.IsEnabled)
                {
                    AddLog("Waiting for SharedMemory... (will start automatically when connected)");
                    _sharedMemoryRetryTimer.Start();
                }
                return;
            }

            _sharedMemoryRetryTimer.Stop();

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
            TimeSpan sendInterval = TimeSpan.FromMilliseconds(_config.SendInterval);
            _sendTimer.Change(sendInterval, sendInterval);
            SendTick();
        }
        catch (Exception ex)
        {
            AddLog(new WorkflowStartException(ex).Message);
            Stop();
        }
        finally
        {
            State.IsOpening = false;
        }
    }

    public void Stop()
    {
        _sharedMemoryRetryTimer.Stop();
        _sendTimer.Change(Timeout.Infinite, Timeout.Infinite);
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
        int currentUdpPort = TryParseUdpPort(State.UdpPortText, out int parsedUdpPort)
            ? parsedUdpPort
            : NormalizeUdpPort(_config.UdpPort);
        UdpTransportOptions currentUdpTransportOptions = _config.UdpTransportOptions.WithFallbackPort(currentUdpPort);

        if (!_portBaudRateSettingsDialogService.TryShow(
                currentPortBaudRates,
                BaudRateOptions,
                currentSentenceUdpPorts,
                currentUdpPort,
                currentUdpTransportOptions,
                _projectSentenceFrameService.SupportsPerSentenceMulticastAddress(_config.ProjectType),
                out IReadOnlyDictionary<string, int> updatedPortBaudRates,
                out IReadOnlyDictionary<string, int> updatedSentenceUdpPorts,
                out IReadOnlyDictionary<string, string> updatedSentenceUdpAddresses,
                out IReadOnlyDictionary<string, double> updatedSentenceHz,
                out int updatedUdpPort,
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

        if (!TryApplySentenceHzSettings(updatedSentenceHz, out string hzError))
        {
            AddLog($"Sentence Hz setting failed: {hzError}");
            return;
        }

        int normalizedUdpPort = NormalizeUdpPort(updatedUdpPort);
        State.UdpPortText = normalizedUdpPort.ToString(CultureInfo.InvariantCulture);
        _config.UdpPort = normalizedUdpPort;

        _config.UdpTransportOptions = normalizedUdpTransportOptions;
        SaveConfig();

        if (State.IsRunning)
        {
            _nmeaTransmissionService.HandleUdpToggleDuringRun(State.IsRunning, State.IsOpening, false, _config.UdpTransportOptions, AddLog);
            SyncUdpOutputStateDuringRun();
        }
    }

    public void OpenSummary()
    {
        IReadOnlyList<TransmissionSourceSummaryItem> items = BuildTransmissionSummaryItems();
        IReadOnlyDictionary<string, string> updatedNotes = _transmissionSummaryDialogService.Show(items);
        if (TryApplySourceNotes(updatedNotes))
        {
            SaveConfig();
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
        foreach (KeyValuePair<SentenceItem, string> sentencePort in previousSentencePorts)
        {
            sentencePort.Key.PortName = _serialPortCatalogService.PickAvailablePort(State.Ports, sentencePort.Value, State.DefaultPort);
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
        _sendTimer.Dispose();
    }

    private async void SendTick()
    {
        // Skip this tick if previous send is still in progress.
        if (Interlocked.CompareExchange(ref _isSending, 1, 0) != 0)
        {
            return;
        }

        try
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

            // Phase 1 (UI thread): compose sentences and update preview texts.
            List<string> composeLogs = new();
            IReadOnlyList<SentenceSendTask> sendTasks = _nmeaTransmissionService.ComposeTick(tickContext, composeLogs.Add);
            State.Logs.AddRange(composeLogs, maxCount: 1000);

            if (sendTasks.Count == 0)
            {
                return;
            }

            // Phase 2 (background thread): perform actual COM/UDP I/O.
            var dispatcher = System.Windows.Application.Current.Dispatcher;
            Action uiStop = () => dispatcher.BeginInvoke(Stop);
            List<string> sendLogs = new();
            await Task.Run(() => _nmeaTransmissionService.ExecuteSend(sendTasks, sendLogs.Add, uiStop));
            State.Logs.AddRange(sendLogs, maxCount: 1000);
        }
        catch (Exception ex)
        {
            AddLog($"Send error: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _isSending, 0);
        }
    }

    private async void OnSharedMemoryRetryTick(object? sender, EventArgs e)
    {
        if (!State.IsIosSource || State.IsRunning || State.IsOpening)
        {
            _sharedMemoryRetryTimer.Stop();
            return;
        }

        if (_sharedMemoryDataProvider.TryRead(out _, out _))
        {
            _sharedMemoryRetryTimer.Stop();
            await StartAsync();
        }
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
            source.Hz,
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
                if (!State.IsIosSource)
                {
                    _sharedMemoryRetryTimer.Stop();
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
                ResolveSentenceUdpAddress(item.UdpAddress, _config.UdpTransportOptions.MulticastAddress),
                item.Hz));
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
                error = new InvalidUdpPortException().Message;
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
                    error = new MulticastAddressRangeException().Message;
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

    private bool TryApplySentenceHzSettings(IReadOnlyDictionary<string, double> sentenceHz, out string error)
    {
        error = string.Empty;
        Dictionary<string, double> normalizedHz = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string rowKey, double hz) in sentenceHz)
        {
            if (string.IsNullOrWhiteSpace(rowKey))
            {
                continue;
            }

            if (hz < SentenceItem.MinHz)
            {
                error = new InvalidSentenceHzException().Message;
                return false;
            }

            normalizedHz[rowKey] = hz;
        }

        foreach ((SentenceItem item, string rowKey, _) in EnumerateConfigurableSentenceRows())
        {
            if (normalizedHz.TryGetValue(rowKey, out double hz))
            {
                item.Hz = hz;
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
                error = new InvalidMulticastAddressException().Message;
                return false;
            }

            if (!IsMulticastAddress(address))
            {
                error = new MulticastAddressRangeException().Message;
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

    private IReadOnlyList<TransmissionSourceSummaryItem> BuildTransmissionSummaryItems()
    {
        List<TransmissionSourceSummaryItem> result = new();

        IEnumerable<IGrouping<string, SentenceItem>> comGroups = State.ConfigurableSentences()
            .Where(item => item.IsComEnabled && !string.IsNullOrWhiteSpace(item.PortName))
            .GroupBy(item => item.PortName.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (IGrouping<string, SentenceItem> group in comGroups)
        {
            string portName = group.Key;
            int baudRate = _portBaudRateService.ResolveBaudRate(_config, portName);
            string sourceKey = BuildComSourceKey(portName);
            string memo = _config.SourceNotes.TryGetValue(sourceKey, out string? configuredMemo)
                ? configuredMemo
                : string.Empty;

            result.Add(new TransmissionSourceSummaryItem(
                sourceKey,
                portName,
                $"BaudRate {baudRate}",
                string.Empty,
                group.Select(item => item.Label),
                memo));
        }

        UdpTransportMode udpMode = _config.UdpTransportOptions.Mode;
        IEnumerable<IGrouping<string, SentenceItem>> udpGroups = State.ConfigurableSentences()
            .Where(item => item.IsUdpEnabled)
            .GroupBy(
                item => BuildUdpGroupKey(
                    NormalizeUdpPort(item.UdpPort),
                    ResolveSentenceUdpAddress(item.UdpAddress, _config.UdpTransportOptions.MulticastAddress),
                    udpMode),
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (IGrouping<string, SentenceItem> group in udpGroups)
        {
            SentenceItem first = group.First();
            int udpPort = NormalizeUdpPort(first.UdpPort);
            string udpAddress = ResolveSentenceUdpAddress(first.UdpAddress, _config.UdpTransportOptions.MulticastAddress);
            string sourceKey = BuildUdpSourceKey(udpPort, udpAddress, udpMode);
            string memo = _config.SourceNotes.TryGetValue(sourceKey, out string? configuredMemo)
                ? configuredMemo
                : string.Empty;
            string secondaryText = udpMode == UdpTransportMode.Multicast
                ? $"Address {udpAddress}"
                : string.Empty;

            result.Add(new TransmissionSourceSummaryItem(
                sourceKey,
                $"UDP {udpPort}",
                $"UDP Port {udpPort}",
                secondaryText,
                group.Select(item => item.Label),
                memo));
        }

        return result;
    }

    private bool TryApplySourceNotes(IReadOnlyDictionary<string, string> sourceNotes)
    {
        Dictionary<string, string> normalized = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string rawKey, string rawValue) in sourceNotes)
        {
            string key = (rawKey ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            string value = rawValue ?? string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            normalized[key] = value;
        }

        bool changed = _config.SourceNotes.Count != normalized.Count ||
                       _config.SourceNotes.Any(pair => !normalized.TryGetValue(pair.Key, out string? value) ||
                                                       !string.Equals(pair.Value, value, StringComparison.Ordinal));

        if (!changed)
        {
            return false;
        }

        _config.SourceNotes.Clear();
        foreach ((string key, string value) in normalized)
        {
            _config.SourceNotes[key] = value;
        }

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
            AddLog(new WorkflowConfigSaveException(ex).Message);
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

        error = new InvalidUdpPortException().Message;
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

    private static string BuildComSourceKey(string portName)
    {
        return $"COM:{(portName ?? string.Empty).Trim().ToUpperInvariant()}";
    }

    private static string BuildUdpGroupKey(int udpPort, string udpAddress, UdpTransportMode udpMode)
    {
        return udpMode == UdpTransportMode.Multicast
            ? $"{udpPort}|{udpAddress}"
            : udpPort.ToString(CultureInfo.InvariantCulture);
    }

    private static string BuildUdpSourceKey(int udpPort, string udpAddress, UdpTransportMode udpMode)
    {
        return udpMode == UdpTransportMode.Multicast
            ? $"UDP:{udpPort}@{udpAddress}"
            : $"UDP:{udpPort}";
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
