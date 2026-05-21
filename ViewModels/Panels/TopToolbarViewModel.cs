using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.ViewModels;

public sealed class TopToolbarViewModel : ObservableObject, IDisposable
{
    private readonly MainStateStore _state;
    private readonly IMainWorkflowService _workflow;

    public TopToolbarViewModel(MainStateStore state, IMainWorkflowService workflow)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));

        StartCommand = new AsyncRelayCommand(_workflow.StartAsync, CanStart);
        StopCommand = new RelayCommand(_workflow.Stop, CanStop);
        RefreshPortsCommand = new RelayCommand(_workflow.RefreshPorts);
        ApplyDefaultPortCommand = new RelayCommand(_workflow.ApplyDefaultPort);
        ApplyDefaultUdpPortCommand = new RelayCommand(_workflow.ApplyDefaultUdpPort);
        OpenSettingsCommand = new RelayCommand(_workflow.OpenSettings);
        ExitCommand = new RelayCommand(_workflow.Exit);

        _state.PropertyChanged += State_PropertyChanged;
    }

    public ObservableCollection<string> Ports => _state.Ports;

    public bool IsComSettingsEditable => _state.IsComSettingsEditable;

    public string DefaultPort
    {
        get => _state.DefaultPort;
        set => _state.DefaultPort = value;
    }

    public bool IsIosSource
    {
        get => _state.IsIosSource;
        set => _state.IsIosSource = value;
    }

    public bool IsTestSource
    {
        get => _state.IsTestSource;
        set => _state.IsTestSource = value;
    }

    public bool UseTrueWind
    {
        get => _state.UseTrueWind;
        set => _state.UseTrueWind = value;
    }

    public string UdpPortText
    {
        get => _state.UdpPortText;
        set => _state.UdpPortText = value;
    }

    public IAsyncRelayCommand StartCommand { get; }

    public IRelayCommand StopCommand { get; }

    public IRelayCommand RefreshPortsCommand { get; }

    public IRelayCommand ApplyDefaultPortCommand { get; }

    public IRelayCommand ApplyDefaultUdpPortCommand { get; }

    public IRelayCommand OpenSettingsCommand { get; }

    public IRelayCommand ExitCommand { get; }

    public void Dispose()
    {
        _state.PropertyChanged -= State_PropertyChanged;
    }

    private bool CanStart()
    {
        return !_state.IsRunning && !_state.IsOpening;
    }

    private bool CanStop()
    {
        return _state.IsRunning;
    }

    private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.PropertyName))
        {
            return;
        }

        OnPropertyChanged(e.PropertyName);
        if (e.PropertyName is nameof(MainStateStore.IsRunning) or nameof(MainStateStore.IsOpening))
        {
            OnPropertyChanged(nameof(IsComSettingsEditable));
            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
    }
}
