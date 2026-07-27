using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace NMEASender.Wpf.Models.UI;

public sealed partial class TransmissionSourceSummaryItem : ObservableObject
{
    public TransmissionSourceSummaryItem(
        string sourceKey,
        string displayName,
        string summaryText,
        string secondaryText,
        IEnumerable<string> sentences,
        string memo)
    {
        SourceKey = sourceKey;
        DisplayName = displayName;
        SummaryText = summaryText;
        SecondaryText = secondaryText;
        _memo = memo ?? string.Empty;

        foreach (string sentence in sentences
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            Sentences.Add(sentence);
        }
    }

    public string SourceKey { get; }

    public string DisplayName { get; }

    public string SummaryText { get; }

    public string SecondaryText { get; }

    public bool HasSecondaryText => !string.IsNullOrWhiteSpace(SecondaryText);

    public bool HasMemo => !string.IsNullOrWhiteSpace(Memo);

    public int SentenceCount => Sentences.Count;

    public ObservableCollection<string> Sentences { get; } = new();

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private string _memo = string.Empty;
}
