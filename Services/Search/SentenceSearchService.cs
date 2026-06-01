using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Search;

namespace NMEASender.Wpf.Services.Search;

public sealed class SentenceSearchService : ISentenceSearchService
{
    public bool MatchesSentence(SentenceItem? sentence, string? rawKeyword)
    {
        if (sentence is null)
        {
            return false;
        }

        string keyword = NormalizeKeyword(rawKeyword);
        if (keyword.Length == 0)
        {
            return true;
        }

        return ContainsIgnoreCase(sentence.Label, keyword) ||
               ContainsIgnoreCase(sentence.Id.ToString(), keyword) ||
               ContainsIgnoreCase(sentence.PrimaryText, keyword) ||
               ContainsIgnoreCase(sentence.SecondaryText, keyword);
    }

    public IEnumerable<SentenceUdpPortItem> FilterSentenceUdpPorts(
        IEnumerable<SentenceUdpPortItem> source,
        string? rawKeyword)
    {
        string keyword = NormalizeKeyword(rawKeyword);
        if (keyword.Length == 0)
        {
            return source;
        }

        return source.Where(item =>
            ContainsIgnoreCase(item.RowKey, keyword) ||
            ContainsIgnoreCase(item.SentenceLabel, keyword) ||
            ContainsIgnoreCase(item.UdpAddress, keyword) ||
            item.UdpPort.ToString().Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeKeyword(string? rawKeyword)
    {
        return (rawKeyword ?? string.Empty).Trim();
    }

    private static bool ContainsIgnoreCase(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source) &&
               source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
