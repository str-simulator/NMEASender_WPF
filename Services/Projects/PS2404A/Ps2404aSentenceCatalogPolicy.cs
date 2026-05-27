using NMEASender.Wpf.Models.Projects;

namespace NMEASender.Wpf.Services.Projects.PS2404A;

public sealed class PS2404ASentenceCatalogPolicy : BaseProjectSentenceCatalogPolicy
{
    public override ProjectType ProjectType => ProjectType.PS2404A;
}
