using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.UI;

namespace NMEASender.Wpf.Services.Interfaces.Transmission;

public interface ISentenceComposerService
{
    IReadOnlyList<string> ComposeAndApplyPreview(
        SentenceItem item,
        NmeaDataDto data,
        bool isIosSource,
        NmeaBuildOptions options);

    bool ShouldSend(SentenceItem item, bool isIosSource, NmeaDataDto data);
}
