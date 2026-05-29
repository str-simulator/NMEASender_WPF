using NMEASender.Wpf.Services.Interfaces.Config;
using System.IO;
using NMEASender.Wpf.Exceptions;

namespace NMEASender.Wpf.Services.Config;

public sealed class IniFileService : IIniFileService
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    public static IniFileService Load(string path)
    {
        IniFileService ini = new();
        if (!File.Exists(path))
        {
            return ini;
        }

        string currentSection = string.Empty;
        foreach (string rawLine in File.ReadAllLines(path))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                ini.GetSection(currentSection);
                continue;
            }

            int equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }

            string key = line[..equalsIndex].Trim();
            string value = line[(equalsIndex + 1)..].Trim();
            ini.Set(currentSection, key, value);
        }

        return ini;
    }

    public string Get(string section, string key, string defaultValue)
    {
        return _sections.TryGetValue(section, out Dictionary<string, string>? values) && values.TryGetValue(key, out string? value)
            ? value
            : defaultValue;
    }

    public int GetInt(string section, string key, int defaultValue)
    {
        return int.TryParse(Get(section, key, defaultValue.ToString()), out int value) ? value : defaultValue;
    }

    public uint GetUInt(string section, string key, uint defaultValue)
    {
        return uint.TryParse(Get(section, key, defaultValue.ToString()), out uint value) ? value : defaultValue;
    }

    public bool GetBool(string section, string key, bool defaultValue)
    {
        string fallback = defaultValue ? "1" : "0";
        string value = Get(section, key, fallback);
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public void Set(string section, string key, string value)
    {
        GetSection(section)[key] = value;
    }

    public void RemoveSection(string section)
    {
        _sections.Remove(section);
    }

    public void MergeFrom(IIniFileService other)
    {
        if (other is not IniFileService source)
        {
            throw new UnsupportedIniImplementationException(other);
        }

        foreach (KeyValuePair<string, Dictionary<string, string>> sectionPair in source._sections)
        {
            string sectionName = sectionPair.Key;
            Dictionary<string, string> section = sectionPair.Value;
            foreach (KeyValuePair<string, string> valuePair in section)
            {
                Set(sectionName, valuePair.Key, valuePair.Value);
            }
        }
    }

    public IReadOnlyDictionary<string, string> GetSectionValues(string section)
    {
        if (_sections.TryGetValue(section, out Dictionary<string, string>? values))
        {
            return new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public void Save(string path)
    {
        List<string> lines = new();
        foreach (KeyValuePair<string, Dictionary<string, string>> sectionPair in _sections)
        {
            string sectionName = sectionPair.Key;
            Dictionary<string, string> section = sectionPair.Value;
            if (sectionName.Length > 0)
            {
                lines.Add($"[{sectionName}]");
            }

            foreach (KeyValuePair<string, string> valuePair in section)
            {
                lines.Add($"{valuePair.Key}={valuePair.Value}");
            }

            lines.Add(string.Empty);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);
        File.WriteAllLines(path, lines);
    }

    private Dictionary<string, string> GetSection(string section)
    {
        if (!_sections.TryGetValue(section, out Dictionary<string, string>? values))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = values;
        }

        return values;
    }
}
