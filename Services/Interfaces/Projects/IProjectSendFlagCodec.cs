using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectSendFlagCodec
{
    ProjectType ProjectType { get; }

    ulong DefaultRawSendFlag(ulong defaultRawValue);

    NmeaSendFlag Decode(ulong rawFlag);

    ulong Encode(NmeaSendFlag flag);
}
