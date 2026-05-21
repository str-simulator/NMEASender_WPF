using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.ViewModels;

public sealed class ManualLogViewModel : ObservableObject, IDisposable
{
    private readonly MainStateStore _state;
    private readonly IMainWorkflowService _workflow;

    public ManualLogViewModel(MainStateStore state, IMainWorkflowService workflow)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));

        GetDataCommand = new RelayCommand(_workflow.GetData);
        SetDataCommand = new RelayCommand(_workflow.SetData);
        ClearLogCommand = new RelayCommand(_workflow.ClearLog);

        _state.PropertyChanged += State_PropertyChanged;
    }

    public string LongitudeText
    {
        get => _state.LongitudeText;
        set => _state.LongitudeText = value;
    }

    public string LatitudeText
    {
        get => _state.LatitudeText;
        set => _state.LatitudeText = value;
    }

    public string SpeedText
    {
        get => _state.SpeedText;
        set => _state.SpeedText = value;
    }

    public string HeadingText
    {
        get => _state.HeadingText;
        set => _state.HeadingText = value;
    }

    public ObservableCollection<string> Logs => _state.Logs;

    public IRelayCommand GetDataCommand { get; }

    public IRelayCommand SetDataCommand { get; }

    public IRelayCommand ClearLogCommand { get; }

    public void Dispose()
    {
        _state.PropertyChanged -= State_PropertyChanged;
    }

    private void State_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(e.PropertyName))
        {
            OnPropertyChanged(e.PropertyName);
        }
    }
}
