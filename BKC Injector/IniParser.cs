using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace BKC_Injector
{
    public partial class IniParser
    {
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
            string currentSectionName = "";

            foreach (var line in File.ReadAllLines(filePath))
            {
                string processedLine = line.Trim();
                if (string.IsNullOrEmpty(processedLine) || processedLine.StartsWith(';') || processedLine.StartsWith('#'))
                    continue;

                if (processedLine.StartsWith('[') && processedLine.EndsWith(']'))
                {
                    if (!string.IsNullOrEmpty(currentSectionName))
                        data[currentSectionName] = new Dictionary<string, string>(currentSection);

                    currentSectionName = processedLine[1..^1].Trim();
                    currentSection.Clear();
                    continue;
                }

                var match = PropertyRegex().Match(processedLine);
                if (match.Success)
                {
                    string key = match.Groups["key"].Value.Trim();
                    string value = match.Groups["value"].Value.Trim();
                    currentSection[key] = value;
                }
            }

            if (!string.IsNullOrEmpty(currentSectionName))
                data[currentSectionName] = new Dictionary<string, string>(currentSection);
        }

        public string? GetValue(string section, string key)
        {
            if (data.TryGetValue(section, out var sectionData) && sectionData.TryGetValue(key, out var value))
                return value;
            return null;
        }

        public void SetValue(string section, string key, string value)
        {
            if (!data.TryGetValue(section, out var sectionData))
            {
                sectionData = [];
                data[section] = sectionData;
            }
            sectionData[key] = value;
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
}
