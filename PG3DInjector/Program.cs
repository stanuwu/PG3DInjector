using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PG3DInjector;

HttpClient client = new();

Console.WriteLine("Preparing to inject...");

if (!IsProcessOpen("Pixel Gun 3D"))
{
    Console.WriteLine("Pixel Gun 3D is not open. Please start the game before injecting.");
    KeepConsoleOpen();
    return;
}

string[] dllNames = { "minhook.x64.dll", "PixelGunCheat.dll" };
string latestReleaseApiUrl = "https://api.github.com/repos/stanuwu/PixelGunCheatInternal/releases/latest";

client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows; Windows NT 10.0; Win64; x64; en-US) AppleWebKit/536.26 (KHTML, like Gecko) Chrome/49.0.3165.319 Safari/533.5 Edge/13.63869");

try
{
    string latestReleaseJson = await GetLatestReleaseJson(latestReleaseApiUrl);
    JObject releaseInfo = JObject.Parse(latestReleaseJson);

    if (releaseInfo["assets"] is JArray assets)
    {
        foreach (var dllName in dllNames)
        {
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            if (!File.Exists(dllPath))
            {
                string? downloadUrl = FindAssetDownloadUrl(assets, dllName);
                if (!string.IsNullOrEmpty(downloadUrl))
                {
                    Console.WriteLine($"Downloading {dllName}...");
                    await DownloadFile(downloadUrl, dllPath);
                }
                else
                {
                    Console.WriteLine($"Failed to find download URL for {dllName} in the latest release.");
                    KeepConsoleOpen();
                    return;
                }
            }
        }

        foreach (var dllName in dllNames)
        {
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            Console.WriteLine($"Injecting {dllName} into Pixel Gun 3D...");
            Inject.Load("Pixel Gun 3D", dllPath);
        }
        Console.WriteLine("Injection completed successfully.");
        Thread.Sleep(3000);
    }
    else
    {
        Console.WriteLine("No assets found in the latest release.");
        KeepConsoleOpen();
    }
}
catch (Exception e)
{
    Console.WriteLine($"Failed to download the latest DLLs or inject: {e.Message}");
    KeepConsoleOpen();
}

async Task<string> GetLatestReleaseJson(string apiUrl)
{
    HttpResponseMessage response = await client.GetAsync(apiUrl);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}

string? FindAssetDownloadUrl(JArray? assets, string assetName)
{
    if (assets == null) return null;

    foreach (var asset in assets)
    {
        if (asset?["name"]?.ToString().Equals(assetName, StringComparison.OrdinalIgnoreCase) == true)
        {
            return asset["browser_download_url"]?.ToString();
        }
    }
    return null;
}

async Task DownloadFile(string url, string outputPath)
{
    HttpResponseMessage response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
    byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
    await File.WriteAllBytesAsync(outputPath, fileBytes);
    Console.WriteLine($"{Path.GetFileName(outputPath)} downloaded successfully.");
}

bool IsProcessOpen(string name)
{
    foreach (Process clsProcess in Process.GetProcesses())
    {
        if (clsProcess.ProcessName.Contains(name))
        {
            return true;
        }
    }
    return false;
}

void KeepConsoleOpen()
{
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
