using System.IO;
using System.Text.RegularExpressions;

namespace BKC_Injector
{
    public partial class IniParser
    {
        private readonly Dictionary<string, Dictionary<string, string>> data;

        public IniParser(string filePath)
        {
            data = [];
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
                    if (currentSection.Count > 0 && !string.IsNullOrEmpty(currentSectionName))
                    {
                        data[currentSectionName] = new Dictionary<string, string>(currentSection);
                        currentSection.Clear();
                    }

                    currentSectionName = processedLine[1..^1].Trim();
                    continue;
                }

                var keyValueMatch = VariableRegex().Match(processedLine);
                if (keyValueMatch.Success)
                {
                    string key = keyValueMatch.Groups["key"].Value.Trim();
                    string value = keyValueMatch.Groups["value"].Value.Trim();
                    currentSection[key] = value;
                }
            }

            if (currentSection.Count > 0 && !string.IsNullOrEmpty(currentSectionName))
            {
                data[currentSectionName] = new Dictionary<string, string>(currentSection);
            }
        }

        public string GetValue(string section, string key)
        {
            if (data.TryGetValue(section, out var sectionData))
            {
                if (sectionData.TryGetValue(key, out var value))
                    return value;
                else
                    throw new KeyNotFoundException($"The key '{key}' was not found in section '{section}'.");
            }
            else
            {
                throw new KeyNotFoundException($"The section '{section}' was not found in the INI file.");
            }
        }

        [GeneratedRegex(@"^(?<key>[^=]+)=(?<value>.*)$")]
        private static partial Regex VariableRegex();
    }
}
