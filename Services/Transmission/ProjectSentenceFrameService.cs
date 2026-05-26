using NMEASender.Wpf.Models;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services;

public sealed class ProjectSentenceFrameService : IProjectSentenceFrameService
{
    private readonly IReadOnlyDictionary<ProjectType, IProjectSentenceFramePolicy> _projectPolicies;
    private readonly IProjectSentenceFramePolicy _fallbackPolicy;

    public ProjectSentenceFrameService(IEnumerable<IProjectSentenceFramePolicy> projectPolicies)
    {
        if (projectPolicies is null)
        {
            throw new ArgumentNullException(nameof(projectPolicies));
        }

        List<IProjectSentenceFramePolicy> policies = projectPolicies.ToList();
        if (policies.Count == 0)
        {
            throw new InvalidOperationException("At least one sentence frame policy must be registered.");
        }

        _projectPolicies = policies
            .GroupBy(policy => policy.ProjectType)
            .ToDictionary(group => group.Key, group => group.First());

        _fallbackPolicy = _projectPolicies.TryGetValue(ProjectType.PS000, out IProjectSentenceFramePolicy? ps000Policy)
            ? ps000Policy
            : policies[0];
    }

    public void Reset(ProjectType projectType, bool rightRpmFirst)
    {
        Resolve(projectType).Reset(rightRpmFirst);
    }

    public bool SupportsPerSentenceMulticastAddress(ProjectType projectType)
    {
        return Resolve(projectType).SupportsPerSentenceMulticastAddress;
    }

    public IReadOnlyList<SentenceItem> SelectForDispatch(
        IReadOnlyList<SentenceItem> enabledSentences,
        ProjectType projectType)
    {
        return Resolve(projectType).SelectForDispatch(enabledSentences);
    }

    public IReadOnlyList<string> ExpandForTransmit(
        IReadOnlyList<string> sentences,
        NmeaSentenceId sentenceId,
        ProjectType projectType)
    {
        return Resolve(projectType).ExpandForTransmit(sentences, sentenceId);
    }

    public int ResolveUdpPort(SentenceItem item, int defaultUdpPort, ProjectType projectType)
    {
        return Resolve(projectType).ResolveUdpPort(item, defaultUdpPort);
    }

    public string? ResolveUdpAddress(SentenceItem item, UdpTransportOptions options, ProjectType projectType)
    {
        return Resolve(projectType).ResolveUdpAddress(item, options);
    }

    private IProjectSentenceFramePolicy Resolve(ProjectType projectType)
    {
        return _projectPolicies.TryGetValue(projectType, out IProjectSentenceFramePolicy? policy)
            ? policy
            : _fallbackPolicy;
    }
}
