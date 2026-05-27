using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Projects;

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
        SentenceItem? rpmPort = null;
        SentenceItem? rpmStbd = null;

        foreach (SentenceItem item in enabledSentences)
        {
            if (item.Id == NmeaSentenceId.RpmPort)
            {
                rpmPort = item;
                continue;
            }

            if (item.Id == NmeaSentenceId.RpmStbd)
            {
                rpmStbd = item;
                continue;
            }

            selected.Add(item);
        }

        if (rpmPort is null && rpmStbd is null)
        {
            return selected;
        }

        if (rpmPort is not null && rpmStbd is not null)
        {
            selected.Add(_sendStarboardRpm ? rpmStbd : rpmPort);
            _sendStarboardRpm = !_sendStarboardRpm;
            return selected;
        }

        selected.Add(rpmStbd ?? rpmPort!);
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
