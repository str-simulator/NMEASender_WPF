namespace NMEASender.Wpf.Services.Interfaces;

public interface ISerialPortCatalogService
{
    IReadOnlyList<string> GetSortedPorts(out string error);

    string PickAvailablePort(IEnumerable<string> availablePorts, string requestedPort, string defaultPort);
}
