using System.IO.Ports;
using NMEASender.Wpf.Models;

namespace NMEASender.Wpf.Services.Interfaces;

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

    NmeaSendFlag SendFlag { get; set; }

    Dictionary<NmeaSentenceId, string> SentencePorts { get; }

    Dictionary<NmeaSentenceId, List<string>> SentencePortRows { get; }

    Dictionary<string, int> PortBaudRates { get; }

    string SavePath { get; set; }

    void Save(IEnumerable<SentenceItem> items);
}
