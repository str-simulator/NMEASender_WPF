using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

public interface IProjectSentenceCatalogPolicy
{
    ProjectType ProjectType { get; }

    bool IsTemplateVisible(ProjectType? requiredProjectType);
}
