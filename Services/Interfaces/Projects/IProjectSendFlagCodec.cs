using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IProjectSendFlagCodec
{
    ProjectType ProjectType { get; }

    ulong DefaultRawSendFlag(ulong defaultRawValue);

    NmeaSendFlag Decode(ulong rawFlag);

    ulong Encode(NmeaSendFlag flag);
}
