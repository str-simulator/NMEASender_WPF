using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Services.Interfaces.Projects;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectSentenceCatalogPolicy : IProjectSentenceCatalogPolicy
{
    public abstract ProjectType ProjectType { get; }

    public virtual bool IsTemplateVisible(ProjectType? requiredProjectType)
    {
        return requiredProjectType is null || requiredProjectType == ProjectType;
    }
}
