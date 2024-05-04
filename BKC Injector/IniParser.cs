using System.IO;
using System.Text.RegularExpressions;

namespace BKC_Injector;

public partial class IniParser
{
    private static readonly HashSet<string> validSections = ["BKC Configuration"];
    private static readonly HashSet<string> validKeys = ["AutoUpdate", "ForceVersion", "AutoInject"];
    private readonly Dictionary<string, Dictionary<string, string>> data = [];

    public IniParser(string filePath)
    {
        LoadData(filePath);
    }

    private void LoadData(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("The specified INI file was not found.", filePath);

        var currentSection = new Dictionary<string, string>();
        var currentSectionName = "";

        foreach (var line in File.ReadAllLines(filePath))
        {
            var processedLine = line.Trim();
            if (string.IsNullOrEmpty(processedLine) || processedLine.StartsWith(';') || processedLine.StartsWith('#'))
                continue;

            if (processedLine.StartsWith('[') && processedLine.EndsWith(']'))
            {
                currentSectionName = processedLine[1..^1].Trim();
                if (validSections.Contains(currentSectionName))
                {
                    currentSection = [];
                    data[currentSectionName] = currentSection;
                }
                else
                {
                    currentSectionName = "";
                }

                continue;
            }

            if (!string.IsNullOrEmpty(currentSectionName))
            {
                var match = PropertyRegex().Match(processedLine);
                if (match.Success)
                {
                    var key = match.Groups["key"].Value.Trim();
                    var value = match.Groups["value"].Value.Trim();
                    if (validKeys.Contains(key)) currentSection[key] = value;
                }
            }
        }
    }

    public string? GetValue(string section, string key)
    {
        if (data.TryGetValue(section, out var sectionData) && sectionData.TryGetValue(key, out var value))
            return value;
        return null;
    }

    public void SetValue(string section, string key, string value)
    {
        if (validSections.Contains(section) && validKeys.Contains(key))
        {
            if (!data.TryGetValue(section, out var sectionData))
            {
                sectionData = [];
                data[section] = sectionData;
            }

            sectionData[key] = value;
        }
    }

    public void SaveSettings(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        foreach (var section in data)
        {
            writer.WriteLine($"[{section.Key}]");
            foreach (var kvp in section.Value)
                writer.WriteLine($"{kvp.Key} = {kvp.Value}");
        }
    }

    [GeneratedRegex(@"^(?<key>[^=]+)=(?<value>.*)$")]
    private static partial Regex PropertyRegex();
}