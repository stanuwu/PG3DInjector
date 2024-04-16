using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PG3DInjector;

internal class Program
{
    private static async Task Main(string[] _)
    {
        HttpClient client = new();

        if (!IsProcessOpen("Pixel Gun 3D"))
        {
            Logger.ConsoleWrite("[<ERROR>] Please start the game before running the injector.", ConsoleColor.Red);
            KeepConsoleOpen();
            return;
        }

        string[] dllNames = { "minhook.x64.dll", "PixelGunCheat.dll" };
        string latestReleaseApiUrl = "https://api.github.com/repos/stanuwu/PixelGunCheatInternal/releases/latest";

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows; Windows NT 10.0; Win64; x64; en-US) AppleWebKit/536.26 (KHTML, like Gecko) Chrome/49.0.3165.319 Safari/533.5 Edge/13.63869");

        bool allFilesExist = true;
        foreach (var dllName in dllNames)
        {
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            if (!File.Exists(dllPath))
            {
                allFilesExist = false;
                Logger.ConsoleWrite($"[<WARN>] {dllName} does not exist locally, attempting download.", ConsoleColor.Yellow);
            }
        }

        if (allFilesExist)
        {
            InjectDLLs(dllNames);
        }
        else
        {
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
                                Logger.ConsoleWrite($"[<INFO>] Downloading {dllName}", ConsoleColor.Cyan);
                                await DownloadFile(downloadUrl, dllPath);
                            }
                            else
                            {
                                Logger.ConsoleWrite($"[<ERROR>] Failed to find download URL for {dllName} in the latest release.", ConsoleColor.Red);
                                KeepConsoleOpen();
                                return;
                            }
                        }
                    }

                    InjectDLLs(dllNames);
                }
                else
                {
                    Logger.ConsoleWrite("[<ERROR>] No assets found in the latest release.", ConsoleColor.Red);
                    KeepConsoleOpen();
                }
            }
            catch (Exception e)
            {
                Logger.ConsoleWrite($"[<ERROR>] Failed to download the latest DLLs or inject: {e.Message}", ConsoleColor.Red);
                KeepConsoleOpen();
            }
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
            Logger.ConsoleWrite($"[<OKAY>] {Path.GetFileName(outputPath)} downloaded successfully.", ConsoleColor.Green);
        }

        static void InjectDLLs(string[] dllNames)
        {
            foreach (var dllName in dllNames)
            {
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
                Logger.ConsoleWrite($"[<INFO>] Injecting {dllName}", ConsoleColor.Cyan);
                Injector.Map("Pixel Gun 3D", dllPath);
            }
            Logger.ConsoleWrite("[<OKAY>] Injection completed successfully.", ConsoleColor.Green);
            Thread.Sleep(7500);
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

        static void KeepConsoleOpen()
        {
            Logger.ConsoleWrite("Press any key to exit...");
            Console.ReadKey();
        }
    }
}