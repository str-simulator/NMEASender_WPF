using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.ViewModels;

public sealed partial class PortBaudRateSettingsViewModel : ObservableObject
{
    private static readonly int[] DefaultBaudRates = [1200, 2400, 4800, 9600, 14400, 19200, 38400, 57600, 115200];

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public PortBaudRateSettingsViewModel(
        IReadOnlyDictionary<string, int> portBaudRates,
        IReadOnlyList<int>? baudRateOptions = null)
    {
        BaudRateOptions = (baudRateOptions is { Count: > 0 } ? baudRateOptions : DefaultBaudRates)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        foreach ((string portName, int baudRate) in portBaudRates.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            PortBaudRates.Add(new PortBaudRateItem(portName, baudRate));
        }
    }

    public ObservableCollection<PortBaudRateItem> PortBaudRates { get; } = new();

    public IReadOnlyList<int> BaudRateOptions { get; }

    public IReadOnlyDictionary<string, int> Result { get; private set; } = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<bool>? CloseRequested;

    [RelayCommand]
    private void Save()
    {
        Dictionary<string, int> result = new(StringComparer.OrdinalIgnoreCase);

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

            result[item.PortName] = item.BaudRate;
        }

        ValidationMessage = string.Empty;
        Result = result;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }
}
