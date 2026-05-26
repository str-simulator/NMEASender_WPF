using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface ISharedMemoryProviderService : IDisposable
{
    bool TryRead(out NmeaDataDto data, out string error);
}
