using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Services.Interfaces.Projects;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectSendFlagCodec : IProjectSendFlagCodec
{
    public abstract ProjectType ProjectType { get; }

    public virtual ulong DefaultRawSendFlag(ulong defaultRawValue)
    {
        return defaultRawValue;
    }

    public virtual NmeaSendFlag Decode(ulong rawFlag)
    {
        return (NmeaSendFlag)rawFlag;
    }

    public virtual ulong Encode(NmeaSendFlag flag)
    {
        return (ulong)flag;
    }
}
