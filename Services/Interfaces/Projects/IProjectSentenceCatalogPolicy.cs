using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Interfaces.Projects;

public interface IProjectSentenceCatalogPolicy
{
    ProjectType ProjectType { get; }

    bool IsTemplateVisible(ProjectType? requiredProjectType);
}
