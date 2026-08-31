using NMEASender.Wpf.Models.Core;
using NMEASender.Wpf.Models.Network;
using NMEASender.Wpf.Models.Projects;
using NMEASender.Wpf.Models.UI;
using System.IO.Ports;

namespace NMEASender.Wpf.Services.Interfaces.Config;

public interface INmeaSenderConfigService
{
    string Title { get; set; }

    string DefaultPort { get; set; }

    int BaudRate { get; set; }

    int DataBits { get; set; }

    Parity Parity { get; set; }

    StopBits StopBits { get; set; }

    int SendInterval { get; set; }

    bool RightRpm { get; set; }

    bool TrueWind { get; set; }

    bool UseHdmOutput { get; set; }

    bool UseUdp { get; set; }

    int UdpPort { get; set; }

    UdpTransportOptions UdpTransportOptions { get; set; }

    ProjectType ProjectType { get; set; }

    NmeaSendFlag SendFlag { get; set; }

    NmeaSendFlag UdpSendFlag { get; set; }

    Dictionary<NmeaSentenceId, string> SentencePorts { get; }

    Dictionary<NmeaSentenceId, List<string>> SentencePortRows { get; }

    Dictionary<NmeaSentenceId, List<int>> SentenceUdpPortRows { get; }

    Dictionary<NmeaSentenceId, List<string>> SentenceUdpAddressRows { get; }

    Dictionary<NmeaSentenceId, List<double>> SentenceHzRows { get; }

    Dictionary<NmeaSentenceId, List<string>> SentenceTalkerIdRows { get; }

    Dictionary<string, int> PortBaudRates { get; }

    Dictionary<string, string> SourceNotes { get; }

    string SavePath { get; set; }

    void Save(IEnumerable<SentenceItem> items);
}
