using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface ISentenceComposerService
{
    IReadOnlyList<string> ComposeAndApplyPreview(
        SentenceItem item,
        NmeaDataDto data,
        bool isIosSource,
        NmeaBuildOptions options);

    bool ShouldSend(SentenceItem item, bool isIosSource, NmeaDataDto data);
}
