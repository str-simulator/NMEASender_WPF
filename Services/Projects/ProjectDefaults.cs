using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Projects;

public sealed class DefaultProjectSendFlagCodec : BaseProjectSendFlagCodec
{
    private readonly ProjectType _projectType;

    public DefaultProjectSendFlagCodec(ProjectType projectType)
    {
        _projectType = projectType;
    }

    public override ProjectType ProjectType => _projectType;
}

public sealed class DefaultProjectSentenceCatalogPolicy : BaseProjectSentenceCatalogPolicy
{
    private readonly ProjectType _projectType;

    public DefaultProjectSentenceCatalogPolicy(ProjectType projectType)
    {
        _projectType = projectType;
    }

    public override ProjectType ProjectType => _projectType;
}

public sealed class DefaultProjectSentenceComposerProfile : BaseProjectSentenceComposerProfile
{
    private readonly ProjectType _projectType;

    public DefaultProjectSentenceComposerProfile(ProjectType projectType)
    {
        _projectType = projectType;
    }

    public override ProjectType ProjectType => _projectType;
}

public sealed class DefaultProjectSentenceFramePolicy : BaseProjectSentenceFramePolicy
{
    private readonly ProjectType _projectType;

    public DefaultProjectSentenceFramePolicy(ProjectType projectType)
    {
        _projectType = projectType;
    }

    public override ProjectType ProjectType => _projectType;
}

public sealed class DefaultProjectUdpTransportProfileStore : BaseProjectUdpTransportProfileStore
{
    private readonly ProjectType _projectType;

    public DefaultProjectUdpTransportProfileStore(ProjectType projectType)
    {
        _projectType = projectType;
    }

    public override ProjectType ProjectType => _projectType;
}
