using NMEASender.Wpf.Models.Core;

namespace NMEASender.Wpf.Services.Interfaces.IO;

public interface ISharedMemoryProviderService : IDisposable
{
    bool TryRead(out NmeaDataDto data, out string error);
}
