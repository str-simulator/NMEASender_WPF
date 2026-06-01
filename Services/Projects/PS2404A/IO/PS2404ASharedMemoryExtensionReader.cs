using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.Projects.PS2404A;
using NMEASender.Wpf.Models.Projects.PS2404A.SharedMemory;
using NMEASender.Wpf.Services.Interfaces.Projects;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace NMEASender.Wpf.Services.Projects.PS2404A.IO;

public sealed class PS2404ASharedMemoryExtensionReader : IProjectSharedMemoryExtensionReader
{
    public ProjectType ProjectType => ProjectType.PS2404A;

    public void Apply(MemoryMappedViewAccessor view, long ownShipBaseSize, NmeaDataDto data)
    {
        if (!TryReadKoseExtension(view, ownShipBaseSize, out PS2404AKoseExtensionNative extension))
        {
            return;
        }

        data.KoseMode = extension.KoseMode;
        data.KoseSogKnots = SafeDouble(extension.KoseSogKnots);
        data.KoseCog = NormalizeDegrees(SafeDouble(extension.KoseCog));
    }

    private static bool TryReadKoseExtension(
        MemoryMappedViewAccessor view,
        long offset,
        out PS2404AKoseExtensionNative extension)
    {
        extension = default;
        int size = Marshal.SizeOf<PS2404AKoseExtensionNative>();
        if (view.Capacity < offset + size)
        {
            return false;
        }

        extension = ReadStruct<PS2404AKoseExtensionNative>(view, offset);
        return extension.KoseMode is >= 0 and <= PS2404AKoseModes.EngineAndRudder
               && double.IsFinite(extension.KoseSogKnots)
               && double.IsFinite(extension.KoseCog);
    }

    private static T ReadStruct<T>(MemoryMappedViewAccessor view, long offset) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] buffer = new byte[size];
        view.ReadArray(offset, buffer, 0, size);
        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private static double SafeDouble(double value)
    {
        return double.IsFinite(value) ? value : 0.0;
    }

    private static double NormalizeDegrees(double degrees)
    {
        degrees %= 360.0;
        return degrees < 0.0 ? degrees + 360.0 : degrees;
    }
}
