using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.UI;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class PS2404ASentenceFramePolicy : BaseProjectSentenceFramePolicy
{
    private bool _sendStarboardRpm = true;

    public override ProjectType ProjectType => ProjectType.PS2404A;

    public override bool SupportsPerSentenceMulticastAddress => true;

    public override void Reset(bool rightRpmFirst)
    {
        _sendStarboardRpm = rightRpmFirst;
    }

    public override IReadOnlyList<SentenceItem> SelectForDispatch(IReadOnlyList<SentenceItem> enabledSentences)
    {
        if (enabledSentences.Count == 0)
        {
            return enabledSentences;
        }

        List<SentenceItem> selected = new(enabledSentences.Count);
        List<SentenceItem> rpmPortItems = new();
        List<SentenceItem> rpmStbdItems = new();

        foreach (SentenceItem item in enabledSentences)
        {
            if (item.Id == NmeaSentenceId.RpmPort)
            {
                rpmPortItems.Add(item);
                continue;
            }

            if (item.Id == NmeaSentenceId.RpmStbd)
            {
                rpmStbdItems.Add(item);
                continue;
            }

            selected.Add(item);
        }

        if (rpmPortItems.Count == 0 && rpmStbdItems.Count == 0)
        {
            return selected;
        }

        if (rpmPortItems.Count > 0 && rpmStbdItems.Count > 0)
        {
            selected.AddRange(_sendStarboardRpm ? rpmStbdItems : rpmPortItems);
            _sendStarboardRpm = !_sendStarboardRpm;
            return selected;
        }

        selected.AddRange(rpmStbdItems.Count > 0 ? rpmStbdItems : rpmPortItems);
        return selected;
    }

    public override IReadOnlyList<string> ExpandForTransmit(IReadOnlyList<string> sentences, NmeaSentenceId sentenceId)
    {
        if (sentences.Count == 0)
        {
            return sentences;
        }

        List<string> expanded = new(sentences.Count * 2);
        foreach (string sentence in sentences)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                continue;
            }

            if (sentence[0] == '!')
            {
                expanded.Add(sentence);
                expanded.Add(sentence);
                continue;
            }

            if (sentence[0] == '$')
            {
                expanded.Add($"1{sentence}");
                expanded.Add($"2{sentence}");
                continue;
            }

            expanded.Add(sentence);
        }

        return expanded;
    }
}
