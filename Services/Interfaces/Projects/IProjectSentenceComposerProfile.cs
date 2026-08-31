using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Services.Interfaces.Transmission;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectSentenceComposerProfile
{
    ProjectType ProjectType { get; }

    string BuildIosVtgSentence(
        NmeaDataDto data,
        NmeaBuildOptions options,
        INmeaSentenceBuilderService sentenceBuilder,
        string talkerId);
}
