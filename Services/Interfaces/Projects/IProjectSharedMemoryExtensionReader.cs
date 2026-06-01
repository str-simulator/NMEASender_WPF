using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using System.IO.MemoryMappedFiles;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectSharedMemoryExtensionReader
{
    ProjectType ProjectType { get; }

    void Apply(MemoryMappedViewAccessor view, long ownShipBaseSize, NmeaDataDto data);
}
