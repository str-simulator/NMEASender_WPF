using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services.Projects;

public abstract class BaseProjectSentenceCatalogPolicy : IProjectSentenceCatalogPolicy
{
    public abstract ProjectType ProjectType { get; }

    public virtual bool IsTemplateVisible(ProjectType? requiredProjectType)
    {
        return requiredProjectType is null || requiredProjectType == ProjectType;
    }
}
