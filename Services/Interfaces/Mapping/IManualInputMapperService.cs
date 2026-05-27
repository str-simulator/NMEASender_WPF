using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Services.Mapping;

namespace NMEASender.Wpf.Services.Interfaces.Mapping;

public interface IManualInputMapperService
{
    ManualInputValues ToInputValues(NmeaDataDto data);

    NmeaDataDto ApplyToData(NmeaDataDto baseData, ManualInputValues input);
}
