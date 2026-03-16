namespace BinaryLane.Cli.Infrastructure.Configuration;

/// <summary>
/// Reads/writes INI config files compatible with Python's configparser.
/// Format: [section] headers, key = value pairs, # and ; comments.
/// </summary>
public sealed class IniFileSource : IConfigSource
{
    private const string DirName = "binarylane";
    private const string FileName = "config.ini";

    private readonly string? _path;
    private readonly Dictionary<string, Dictionary<string, string>> _sections = new(StringComparer.OrdinalIgnoreCase);
    private string _sectionName = "bl";

    public string SectionName
    {
        get => _sectionName;
        set => _sectionName = value;
    }

    public IniFileSource() : this(GetDefaultPath()) { }

    public IniFileSource(string? path)
    {
        _path = path;
        if (_path != null && File.Exists(_path))
        {
            Parse(File.ReadAllLines(_path));
        }
    }

    public string? Get(string name)
    {
        if (_sections.TryGetValue(_sectionName, out var section))
        {
            return section.GetValueOrDefault(name);
        }
        return null;
    }

    public void Save(Dictionary<string, string?> options)
    {
        if (_path == null) throw new InvalidOperationException("Cannot save when path is null");

        if (!_sections.ContainsKey(_sectionName))
            _sections[_sectionName] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var section = _sections[_sectionName];
        foreach (var (key, value) in options)
        {
            if (value != null)
                section[key] = value;
            else
                section.Remove(key);
        }

        var dir = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(dir);

        using var writer = new StreamWriter(_path);
        foreach (var (sectionName, sectionValues) in _sections)
        {
            writer.WriteLine($"[{sectionName}]");
            foreach (var (key, value) in sectionValues)
            {
                writer.WriteLine($"{key} = {value}");
            }
            writer.WriteLine();
        }
    }

    private void Parse(string[] lines)
    {
        string? currentSection = null;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line[0] is '#' or ';')
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1].Trim();
                if (!_sections.ContainsKey(currentSection))
                    _sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (currentSection == null) continue;

            var eqIndex = line.IndexOf('=');
            if (eqIndex < 0) continue;

            var key = line[..eqIndex].Trim();
            var value = line[(eqIndex + 1)..].Trim();
            _sections[currentSection][key] = value;
        }
    }

    private static string GetDefaultPath()
    {
        string configHome;
        if (OperatingSystem.IsWindows())
        {
            configHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                         ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }
        return Path.Combine(configHome, DirName, FileName);
    }
}
