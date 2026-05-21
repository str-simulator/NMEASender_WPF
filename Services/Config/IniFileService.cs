using System.IO;
using NMEASender.Wpf.Services.Interfaces;

namespace NMEASender.Wpf.Services;

public sealed class IniFileService : IIniFileService
{
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);

    public static IniFileService Load(string path)
    {
        var ini = new IniFileService();
        if (!File.Exists(path))
        {
            return ini;
        }

        var currentSection = string.Empty;
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
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

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex <= 0)
            {
                continue;
            }

            var key = line[..equalsIndex].Trim();
            var value = line[(equalsIndex + 1)..].Trim();
            ini.Set(currentSection, key, value);
        }

        return ini;
    }

    public string Get(string section, string key, string defaultValue)
    {
        return _sections.TryGetValue(section, out var values) && values.TryGetValue(key, out var value)
            ? value
            : defaultValue;
    }

    public int GetInt(string section, string key, int defaultValue)
    {
        return int.TryParse(Get(section, key, defaultValue.ToString()), out var value) ? value : defaultValue;
    }

    public uint GetUInt(string section, string key, uint defaultValue)
    {
        return uint.TryParse(Get(section, key, defaultValue.ToString()), out var value) ? value : defaultValue;
    }

    public bool GetBool(string section, string key, bool defaultValue)
    {
        var fallback = defaultValue ? "1" : "0";
        var value = Get(section, key, fallback);
        return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    public void Set(string section, string key, string value)
    {
        GetSection(section)[key] = value;
    }

    public void MergeFrom(IIniFileService other)
    {
        if (other is not IniFileService source)
        {
            throw new ArgumentException("Unsupported INI implementation.", nameof(other));
        }

        foreach (var (sectionName, section) in source._sections)
        {
            foreach (var (key, value) in section)
            {
                Set(sectionName, key, value);
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
        var lines = new List<string>();
        foreach (var (sectionName, section) in _sections)
        {
            if (sectionName.Length > 0)
            {
                lines.Add($"[{sectionName}]");
            }

            foreach (var (key, value) in section)
            {
                lines.Add($"{key}={value}");
            }

            lines.Add(string.Empty);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);
        File.WriteAllLines(path, lines);
    }

    private Dictionary<string, string> GetSection(string section)
    {
        if (!_sections.TryGetValue(section, out var values))
        {
            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = values;
        }

        return values;
    }
}
