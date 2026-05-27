using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Config;

namespace NMEASender.Wpf.Services.Interfaces.Transmission;

public interface ISentenceCatalogService
{
    void Populate(
        ICollection<SentenceItem> gpsSentences,
        ICollection<SentenceItem> otherSentences,
        ICollection<SentenceItem> internalSentences,
        INmeaSenderConfigService config,
        Func<string, string> pickAvailablePort);
}
