namespace NMEASender.Wpf.Services.Interfaces.Ports;

public interface ISerialPortCatalogService
{
    IReadOnlyList<string> GetSortedPorts(out string error);

    string PickAvailablePort(IEnumerable<string> availablePorts, string requestedPort, string defaultPort);
}
