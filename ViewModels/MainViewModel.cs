using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Threading;
using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services;

namespace NMEASender.Wpf.ViewModels;

public sealed class MainViewModel : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly SerialPortHub _serialPortHub = new();
    private readonly UdpBroadcastSender _udpSender = new();
    private readonly SharedMemoryNmeaDataProvider _sharedMemoryDataProvider = new();
    private readonly SentenceComposerService _sentenceComposer = new();
    private readonly SentenceCatalogService _sentenceCatalog = new();
    private readonly NmeaSenderConfig _config;
    private readonly RelayCommand _startCommand;
    private readonly RelayCommand _stopCommand;
    private readonly RelayCommand _addSentenceRowCommand;
    private readonly RelayCommand _removeSentenceRowCommand;
    private readonly HashSet<string> _openPorts = new(StringComparer.OrdinalIgnoreCase);
    private bool _isRunning;
    private bool _isOpening;
    private bool _sharedMemoryWarningLogged;
    private bool _isTestSource;
    private bool _isIosSource = true;
    private bool _useTrueWind;
    private bool _useHdmOutput;
    private bool _useUdp;
    private bool _areAllSentencesChecked = true;
    private bool _isSynchronizingAllSentencesChecked;
    private string _title;
    private string _defaultPort;
    private string _udpPortText = "40014";
    private string _longitudeText = "129.0000";
    private string _latitudeText = "35.0000";
    private string _speedText = "0.0";
    private string _headingText = "0.0";
    private NmeaDataDto _data = new();

    public MainViewModel()
    {
        _config = NmeaSenderConfig.Load();
        _title = _config.Title;
        _defaultPort = _config.DefaultPort;
        _useTrueWind = _config.TrueWind;
        _useHdmOutput = _config.UseHdmOutput;
        _useUdp = _config.UseUdp;
        _udpPortText = _config.UdpPort.ToString(CultureInfo.InvariantCulture);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_config.SendInterval) };
        _timer.Tick += (_, _) => SendTick();

        _startCommand = new RelayCommand(Start, () => !IsRunning && !IsOpening);
        _stopCommand = new RelayCommand(Stop, () => IsRunning);
        ApplyDefaultPortCommand = new RelayCommand(ApplyDefaultPort);
        RefreshPortsCommand = new RelayCommand(RefreshPorts);
        SetDataCommand = new RelayCommand(SetData);
        GetDataCommand = new RelayCommand(GetData);
        _addSentenceRowCommand = new RelayCommand(AddSentenceRow, _ => IsComSettingsEditable);
        _removeSentenceRowCommand = new RelayCommand(RemoveSentenceRow, CanRemoveSentenceRow);
        ClearLogCommand = new RelayCommand(ClearLog);
        ExitCommand = new RelayCommand(Exit);

        RefreshPorts();
        BuildSentenceRows();
        SetData();
        AddLog("COM Close");

        Start();
    }

    public ObservableCollection<string> Ports { get; } = new();

    public ObservableCollection<string> Logs { get; } = new();

    public ObservableCollection<SentenceItem> GpsSentences { get; } = new();

    public ObservableCollection<SentenceItem> OtherSentences { get; } = new();

    private readonly List<SentenceItem> _internalSentences = new();

    public RelayCommand StartCommand => _startCommand;

    public RelayCommand StopCommand => _stopCommand;

    public RelayCommand ApplyDefaultPortCommand { get; }

    public RelayCommand RefreshPortsCommand { get; }

    public RelayCommand SetDataCommand { get; }

    public RelayCommand GetDataCommand { get; }

    public RelayCommand AddSentenceRowCommand => _addSentenceRowCommand;

    public RelayCommand RemoveSentenceRowCommand => _removeSentenceRowCommand;

    public RelayCommand ClearLogCommand { get; }

    public RelayCommand ExitCommand { get; }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                _startCommand.RaiseCanExecuteChanged();
                _stopCommand.RaiseCanExecuteChanged();
                _addSentenceRowCommand.RaiseCanExecuteChanged();
                _removeSentenceRowCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(IsComSettingsEditable));
            }
        }
    }

    public bool IsOpening
    {
        get => _isOpening;
        private set
        {
            if (SetProperty(ref _isOpening, value))
            {
                _startCommand.RaiseCanExecuteChanged();
                _addSentenceRowCommand.RaiseCanExecuteChanged();
                _removeSentenceRowCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(IsComSettingsEditable));
            }
        }
    }

    public bool IsComSettingsEditable => !IsRunning && !IsOpening;

    public bool AreAllSentencesChecked
    {
        get => _areAllSentencesChecked;
        set
        {
            if (!SetProperty(ref _areAllSentencesChecked, value) || _isSynchronizingAllSentencesChecked)
            {
                return;
            }

            foreach (SentenceItem item in ConfigurableSentences())
            {
                item.IsEnabled = value;
            }
        }
    }

    public bool IsTestSource
    {
        get => _isTestSource;
        set
        {
            if (SetProperty(ref _isTestSource, value) && value)
            {
                IsIosSource = false;
            }
        }
    }

    public bool IsIosSource
    {
        get => _isIosSource;
        set
        {
            if (SetProperty(ref _isIosSource, value) && value)
            {
                IsTestSource = false;
            }
        }
    }

    public bool UseTrueWind
    {
        get => _useTrueWind;
        set
        {
            if (SetProperty(ref _useTrueWind, value))
            {
                GeneratePreview();
            }
        }
    }

    public bool UseUdp
    {
        get => _useUdp;
        set
        {
            if (SetProperty(ref _useUdp, value))
            {
                GeneratePreview();
                HandleUdpToggleDuringRun();
            }
        }
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string DefaultPort
    {
        get => _defaultPort;
        set => SetProperty(ref _defaultPort, (value ?? string.Empty).Trim());
    }

    public string UdpPortText
    {
        get => _udpPortText;
        set => SetProperty(ref _udpPortText, (value ?? string.Empty).Trim());
    }

    public string LongitudeText
    {
        get => _longitudeText;
        set => SetProperty(ref _longitudeText, value);
    }

    public string LatitudeText
    {
        get => _latitudeText;
        set => SetProperty(ref _latitudeText, value);
    }

    public string SpeedText
    {
        get => _speedText;
        set => SetProperty(ref _speedText, value);
    }

    public string HeadingText
    {
        get => _headingText;
        set => SetProperty(ref _headingText, value);
    }

    public void Dispose()
    {
        Stop();
        _serialPortHub.Dispose();
        _udpSender.Dispose();
        _sharedMemoryDataProvider.Dispose();
    }

    private async void Start()
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
            _serialPortHub.CloseAll();
            _udpSender.Close();
            _openPorts.Clear();
            _serialPortHub.Configure(_config.BaudRate, _config.DataBits, _config.Parity, _config.StopBits);

            List<string> enabledPorts = AllSentences()
                .Where(item => item.IsEnabled)
                .Select(item => item.PortName)
                .Where(port => !string.IsNullOrWhiteSpace(port))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (enabledPorts.Count == 0 && !UseUdp)
            {
                AddLog("No COM port selected");
                return;
            }

            if (enabledPorts.Count > 0)
            {
                AddLog($"Opening {enabledPorts.Count} COM port(s)...");
                List<PortOpenResult> openResults = await Task.Run(() => enabledPorts
                    .Select(portName =>
                    {
                        bool success = _serialPortHub.Open(portName, out string? error);
                        return new PortOpenResult(portName, success, error);
                    })
                    .ToList());

                foreach (PortOpenResult result in openResults)
                {
                    if (result.Success)
                    {
                        _openPorts.Add(result.PortName);
                        AddLog($"{result.PortName} Open Success");
                    }
                    else
                    {
                        AddLog($"{result.PortName} Open Fail: {result.Error}");
                    }
                }
            }
            else
            {
                AddLog("No COM port selected; UDP only");
            }

            if (UseUdp)
            {
                if (_udpSender.Open(udpPort, out string? udpError))
                {
                    AddLog($"UDP Broadcast Open: {udpPort}");
                }
                else
                {
                    AddLog($"UDP Broadcast Open Fail: {udpError}");
                }
            }

            if (_openPorts.Count == 0 && !_udpSender.IsOpen)
            {
                AddLog("Send stopped: no output opened.");
                return;
            }

            AddLog(IsIosSource
                ? "By IOS selected: reading STR_OWNSHIP_DATA"
                : "TEST selected: current input values are used");

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

    private void Stop()
    {
        _timer.Stop();
        _serialPortHub.CloseAll();
        _udpSender.Close();
        _openPorts.Clear();
        if (IsRunning)
        {
            AddLog("COM Close");
        }

        IsRunning = false;
    }

    private void Exit()
    {
        SaveConfig();
        Application.Current.MainWindow?.Close();
    }

    private void SendTick()
    {
        if (!UpdateCurrentData())
        {
            return;
        }

        foreach (SentenceItem item in AllSentences().Where(item => item.IsEnabled))
        {
            if (!SentenceComposerService.ShouldSend(item, IsIosSource, _data))
            {
                continue;
            }

            IReadOnlyList<string> sentences = _sentenceComposer.ComposeAndApplyPreview(item, _data, IsIosSource, CurrentBuildOptions());
            if (!string.IsNullOrWhiteSpace(item.PortName) && _openPorts.Contains(item.PortName))
            {
                SendToCom(item, sentences);
            }
            else if (!_udpSender.IsOpen && string.IsNullOrWhiteSpace(item.PortName))
            {
                AddLog($"{item.Label} COM not selected");
            }

            if (_udpSender.IsOpen)
            {
                SendToUdp(item, sentences);
            }
        }
    }

    private void SendToCom(SentenceItem item, IReadOnlyList<string> sentences)
    {
        foreach (string sentence in sentences)
        {
            if (_serialPortHub.Write(item.PortName, sentence, out string? error)) // Send to COM port
            {
                if (item.Id == NmeaSentenceId.STR) // Don't log STR sentences to avoid log spam; they're for internal use only.
                {
                    Debug.WriteLine($"{item.PortName} {sentence.TrimEnd()}"); // Write STR sentences to debug output instead of log.
                    continue;
                }

                AddLog($"{item.PortName} {sentence.TrimEnd()}"); // Log sent sentence
                continue;
            }

            AddLog($"{item.PortName} {item.Label} Send Fail: {error}");
            _openPorts.Remove(item.PortName);
            AddLog($"{item.PortName} disabled for this run.");
            StopIfNoOutputIsOpen();
            break;
        }
    }

    private void SendToUdp(SentenceItem item, IReadOnlyList<string> sentences)
    {
        foreach (string sentence in sentences)
        {
            if (_udpSender.Send(sentence, out string? error))
            {
                AddLog($"UDP:{UdpPortText} {sentence.TrimEnd()}");
                continue;
            }

            AddLog($"UDP:{UdpPortText} {item.Label} Send Fail: {error}");
            _udpSender.Close();
            StopIfNoOutputIsOpen();
            break;
        }
    }

    private void HandleUdpToggleDuringRun()
    {
        if (!IsRunning || IsOpening)
        {
            return;
        }

        if (!UseUdp)
        {
            if (_udpSender.IsOpen)
            {
                _udpSender.Close();
                AddLog("UDP Broadcast Close");
            }

            return;
        }

        if (_udpSender.IsOpen)
        {
            return;
        }

        if (!TryGetUdpPort(out int udpPort, out string? udpPortError))
        {
            AddLog(udpPortError);
            return;
        }

        if (_udpSender.Open(udpPort, out string? udpError))
        {
            AddLog($"UDP Broadcast Open: {udpPort}");
            return;
        }

        AddLog($"UDP Broadcast Open Fail: {udpError}");
    }

    private void StopIfNoOutputIsOpen()
    {
        if (_openPorts.Count > 0 || _udpSender.IsOpen)
        {
            return;
        }

        AddLog("Send stopped: all outputs are closed.");
        Stop();
    }

    private bool UpdateCurrentData(bool forceLog = false)
    {
        if (!IsIosSource)
        {
            _data = ManualInputMapper.ApplyToData(_data, CurrentManualInput());
            _sharedMemoryWarningLogged = false;
            return true;
        }

        if (_sharedMemoryDataProvider.TryRead(out NmeaDataDto? data, out string? error))
        {
            _data = data;
            _sharedMemoryWarningLogged = false;
            ApplyManualInput(ManualInputMapper.ToInputValues(_data));
            return true;
        }

        if (forceLog || !_sharedMemoryWarningLogged)
        {
            AddLog($"SharedMemory Read Fail: {error}");
            _sharedMemoryWarningLogged = true;
        }

        return false;
    }

    private void SetData()
    {
        if (UpdateCurrentData(forceLog: true))
        {
            GeneratePreview();
        }
    }

    private void GetData()
    {
        if (IsIosSource)
        {
            UpdateCurrentData(forceLog: true);
        }

        ApplyManualInput(ManualInputMapper.ToInputValues(_data));
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

    private void ApplyDefaultPort()
    {
        foreach (SentenceItem item in AllSentences())
        {
            // STR sentences are used internally only and use a fixed COM port.
            // Settings can be changed in the ini file.
            if (item.Id == NmeaSentenceId.STR)
            {
                continue;
            }
            item.PortName = DefaultPort;
        }
        SaveConfig();
    }

    private void AddSentenceRow(object? parameter)
    {
        if (parameter is not SentenceItem source)
        {
            return;
        }

        if (TryDuplicateSentenceRow(GpsSentences, source) || TryDuplicateSentenceRow(OtherSentences, source))
        {
            GeneratePreview();
            SaveConfig();
        }
    }

    private void RemoveSentenceRow(object? parameter)
    {
        if (parameter is not SentenceItem source || !source.IsDuplicateRow)
        {
            return;
        }

        if (TryRemoveSentenceRow(GpsSentences, source) || TryRemoveSentenceRow(OtherSentences, source))
        {
            SaveConfig();
        }
    }

    private bool CanRemoveSentenceRow(object? parameter)
    {
        return IsComSettingsEditable && parameter is SentenceItem { IsDuplicateRow: true };
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
        SynchronizeAllSentencesChecked();
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
        SynchronizeAllSentencesChecked();
        return true;
    }

    private static SentenceItem CloneSentenceItem(SentenceItem source)
    {
        return new SentenceItem(source.Id, source.Flag, source.Label, source.PortName, source.IsEnabled, source.HasSecondary, isDuplicateRow: true)
        {
            PrimaryText = source.PrimaryText,
            SecondaryText = source.SecondaryText
        };
    }

    public void RefreshPorts()
    {
        string previousDefault = string.IsNullOrWhiteSpace(DefaultPort) ? _config.DefaultPort : DefaultPort;
        Dictionary<SentenceItem, string> previousSentencePorts = AllSentences().ToDictionary(item => item, item => item.PortName);
        IReadOnlyList<string> names = SerialPortCatalogService.GetSortedPorts(out string? portScanError);
        if (!string.IsNullOrWhiteSpace(portScanError))
        {
            AddLog($"COM scan failed: {portScanError}");
        }

        Ports.Clear();
        foreach (string port in names)
        {
            Ports.Add(port);
        }

        DefaultPort = SerialPortCatalogService.PickAvailablePort(Ports, previousDefault, _config.DefaultPort);
        foreach (var (item, portName) in previousSentencePorts)
        {
            item.PortName = SerialPortCatalogService.PickAvailablePort(Ports, portName, DefaultPort);
        }
    }

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
            port => SerialPortCatalogService.PickAvailablePort(Ports, port, DefaultPort));

        foreach (SentenceItem sentence in ConfigurableSentences())
        {
            sentence.PropertyChanged += Sentence_PropertyChanged;
        }

        SynchronizeAllSentencesChecked();
    }

    private IEnumerable<SentenceItem> AllSentences()
    {
        return GpsSentences.Concat(OtherSentences).Concat(_internalSentences);
    }

    private IEnumerable<SentenceItem> ConfigurableSentences()
    {
        return GpsSentences.Concat(OtherSentences);
    }

    private void Sentence_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SentenceItem.IsEnabled))
        {
            return;
        }

        SynchronizeAllSentencesChecked();
    }

    private void SynchronizeAllSentencesChecked()
    {
        bool isAllChecked = ConfigurableSentences().All(item => item.IsEnabled);

        _isSynchronizingAllSentencesChecked = true;
        try
        {
            AreAllSentencesChecked = isAllChecked;
        }
        finally
        {
            _isSynchronizingAllSentencesChecked = false;
        }
    }

    private void SaveConfig()
    {
        try
        {
            _config.Title = Title;
            _config.DefaultPort = DefaultPort;
            _config.TrueWind = UseTrueWind;
            _config.UseHdmOutput = _useHdmOutput;
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

    private NmeaBuildOptions CurrentBuildOptions()
    {
        return new NmeaBuildOptions(UseTrueWind, _useHdmOutput);
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

    private sealed record PortOpenResult(string PortName, bool Success, string Error);
}
