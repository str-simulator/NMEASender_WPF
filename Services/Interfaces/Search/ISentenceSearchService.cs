using NMEASender.Wpf.Models.UI;

namespace NMEASender.Wpf.Services.Interfaces.Search;

public interface ISentenceSearchService
{
    bool MatchesSentence(SentenceItem? sentence, string? rawKeyword);

    IEnumerable<SentenceUdpPortItem> FilterSentenceUdpPorts(
        IEnumerable<SentenceUdpPortItem> source,
        string? rawKeyword);
}
