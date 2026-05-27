using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.UI;
using NMEASender.Wpf.Services.Interfaces.Projects;
using NMEASender.Wpf.Services.Interfaces.Transmission;

namespace NMEASender.Wpf.Services.Transmission;

public sealed class SentenceComposerService : ISentenceComposerService
{
    private readonly INmeaSentenceBuilderService _sentenceBuilder;
    private readonly IReadOnlyDictionary<ProjectType, IProjectSentenceComposerProfile> _projectProfiles;
    private readonly IProjectSentenceComposerProfile _fallbackProfile;

    public SentenceComposerService(
        INmeaSentenceBuilderService sentenceBuilder,
        IEnumerable<IProjectSentenceComposerProfile> projectProfiles)
    {
        _sentenceBuilder = sentenceBuilder ?? throw new ArgumentNullException(nameof(sentenceBuilder));
        if (projectProfiles is null)
        {
            throw new ArgumentNullException(nameof(projectProfiles));
        }

        List<IProjectSentenceComposerProfile> profiles = projectProfiles.ToList();
        if (profiles.Count == 0)
        {
            throw new InvalidOperationException("At least one sentence composer profile must be registered.");
        }

        _projectProfiles = profiles
            .GroupBy(profile => profile.ProjectType)
            .ToDictionary(group => group.Key, group => group.First());

        _fallbackProfile = _projectProfiles.TryGetValue(ProjectType.PS000, out IProjectSentenceComposerProfile? ps000Profile)
            ? ps000Profile
            : profiles[0];
    }

    public IReadOnlyList<string> ComposeAndApplyPreview(
        SentenceItem item,
        NmeaDataDto data,
        bool isIosSource,
        NmeaBuildOptions options)
    {
        if (item.Id == NmeaSentenceId.Vtg && isIosSource)
        {
            string sentence = ResolveProfile(options.ProjectType)
                .BuildIosVtgSentence(data, options, _sentenceBuilder);
            item.PrimaryText = sentence.TrimEnd();
            item.SecondaryText = string.Empty;
            return new[] { sentence };
        }

        IReadOnlyList<string> sentences = _sentenceBuilder.Build(item.Id, data, options);
        item.PrimaryText = sentences.Count > 0 ? sentences[0].TrimEnd() : string.Empty;
        item.SecondaryText = sentences.Count > 1 ? sentences[1].TrimEnd() : string.Empty;
        return sentences;
    }

    public bool ShouldSend(SentenceItem item, bool isIosSource, NmeaDataDto data)
    {
        if (!isIosSource)
        {
            return true;
        }

        return item.Id switch
        {
            NmeaSentenceId.Gga or NmeaSentenceId.Gll or NmeaSentenceId.Rmc or NmeaSentenceId.Vtg or NmeaSentenceId.Zda or NmeaSentenceId.Gpdtm
                => !data.FailGps,
            NmeaSentenceId.Hdt => !data.FailGyro,
            NmeaSentenceId.Vbw => !data.FailLog,
            NmeaSentenceId.Dbt or NmeaSentenceId.Dpt => !data.FailEcho,
            _ => true
        };
    }

    private IProjectSentenceComposerProfile ResolveProfile(ProjectType projectType)
    {
        return _projectProfiles.TryGetValue(projectType, out IProjectSentenceComposerProfile? profile)
            ? profile
            : _fallbackProfile;
    }
}
