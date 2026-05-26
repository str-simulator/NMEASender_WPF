using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface ISentenceCatalogService
{
    void Populate(
        ICollection<SentenceItem> gpsSentences,
        ICollection<SentenceItem> otherSentences,
        ICollection<SentenceItem> internalSentences,
        INmeaSenderConfigService config,
        Func<string, string> pickAvailablePort);
}
