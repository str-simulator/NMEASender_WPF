using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IManualInputMapperService
{
    ManualInputValues ToInputValues(NmeaDataDto data);

    NmeaDataDto ApplyToData(NmeaDataDto baseData, ManualInputValues input);
}
