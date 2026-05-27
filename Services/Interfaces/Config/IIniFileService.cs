namespace NMEASender.Wpf.Services.Interfaces.Config;

public interface IIniFileService
{
    string Get(string section, string key, string defaultValue);

    int GetInt(string section, string key, int defaultValue);

    uint GetUInt(string section, string key, uint defaultValue);

    bool GetBool(string section, string key, bool defaultValue);

    void Set(string section, string key, string value);

    void MergeFrom(IIniFileService other);

    IReadOnlyDictionary<string, string> GetSectionValues(string section);

    void Save(string path);
}
