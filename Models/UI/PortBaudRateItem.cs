using CommunityToolkit.Mvvm.ComponentModel;

namespace NMEASender.Wpf.Models;

public sealed partial class PortBaudRateItem : ObservableObject
{
    public PortBaudRateItem(string portName, int baudRate)
    {
        PortName = portName;
        _baudRate = baudRate;
    }

    public string PortName { get; }

    [ObservableProperty]
    private int _baudRate;
}
