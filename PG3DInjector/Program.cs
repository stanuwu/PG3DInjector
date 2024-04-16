using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PG3DInjector;

internal class Program
{
    private static readonly string DLLName = "PixelGunCheat.dll";
    private static readonly string LatestReleaseApiUrl = "https://api.github.com/repos/stanuwu/PixelGunCheatInternal/releases/latest";

    private static async Task Main(string[] _)
    {
        using HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");

        var targetProcess = GetFirstNonSuspendedPixelGun3DInstance();
        if (targetProcess == null)
        {
            Logger.ConsoleWrite("[<ERROR>] Please start a non-suspended instance of the game before running the injector.", ConsoleColor.Red);
            KeepConsoleOpen();
            return;
        }

        if (!CheckIfExists(DLLName))
        {
            if (!await TryDownloadMissingFile(client, DLLName))
            {
                KeepConsoleOpen();
                return;
            }
        }

        InjectDLLAndHandleResult(DLLName, targetProcess);
        KeepConsoleOpen();
    }

    private static bool CheckIfExists(string dllName)
    {
        bool allFilesExist = true;
        if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName)))
        {
            allFilesExist = false;
            Logger.ConsoleWrite($"[<WARN>] {dllName} was not found locally, attempting download.", ConsoleColor.Yellow);
        }
        return allFilesExist;
    }

    private static async Task<bool> TryDownloadMissingFile(HttpClient client, string dllName)
    {
        try
        {
            string latestReleaseJson = await GetLatestReleaseJson(client, LatestReleaseApiUrl);
            JObject releaseInfo = JObject.Parse(latestReleaseJson);

            if (releaseInfo["assets"] is JArray assets)
            {
                await DownloadFileIfNeeded(client, assets, dllName);
            }
            else
            {
                Logger.ConsoleWrite("[<ERROR>] No assets found in the latest release.", ConsoleColor.Red);
                return false;
            }
        }
        catch (Exception e)
        {
            Logger.ConsoleWrite($"[<ERROR>] Failed to download the latest DLL: {e.Message}", ConsoleColor.Red);
            return false;
        }
        return true;
    }

    private static async Task DownloadFileIfNeeded(HttpClient client, JArray assets, string dllName)
    {
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        if (!File.Exists(dllPath))
        {
            string? downloadUrl = FindAssetDownloadUrl(assets, dllName);
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                Logger.ConsoleWrite($"[<INFO>] Downloading {dllName}", ConsoleColor.Cyan);
                await DownloadFile(client, downloadUrl, dllPath);
            }
            else
            {
                Logger.ConsoleWrite("[<ERROR>] Failed to find download URL for {dllName} in the latest release.", ConsoleColor.Red);
                throw new InvalidOperationException("Download URL not found.");
            }
        }
    }

    private static async Task<string> GetLatestReleaseJson(HttpClient client, string apiUrl)
    {
        HttpResponseMessage response = await client.GetAsync(apiUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static string? FindAssetDownloadUrl(JArray assets, string assetName)
    {
        foreach (var asset in assets)
        {
            if (asset["name"]?.ToString().Equals(assetName, StringComparison.OrdinalIgnoreCase) == true)
            {
                return asset["browser_download_url"]?.ToString();
            }
        }
        return null;
    }

    private static async Task DownloadFile(HttpClient client, string url, string outputPath)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(outputPath, fileBytes);
        Logger.ConsoleWrite($"[<OKAY>] {Path.GetFileName(outputPath)} downloaded successfully.", ConsoleColor.Green);
    }

    private static void InjectDLLAndHandleResult(string dllName, Process targetProcess)
    {
        if (!TryInjectDll(dllName, targetProcess))
        {
            Logger.ConsoleWrite("[<ERROR>] Injection failed.", ConsoleColor.Red);
        }
        else
        {
            Logger.ConsoleWrite("[<OKAY>] Injection completed successfully.", ConsoleColor.Green);
        }
    }

    private static bool TryInjectDll(string dllName, Process targetProcess)
    {
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        Logger.ConsoleWrite($"[<INFO>] Injecting {dllName} into process {targetProcess.Id}", ConsoleColor.Cyan);
        try
        {
            Injector.Map(targetProcess.ProcessName, dllPath);
            return true;
        }
        catch (Exception ex)
        {
            Logger.ConsoleWrite($"[<ERROR>] Failed to inject {dllName} into process {targetProcess.Id}: {ex.Message}", ConsoleColor.Red);
            return false;
        }
    }

    private static Process? GetFirstNonSuspendedPixelGun3DInstance()
    {
        var processes = Process.GetProcessesByName("Pixel Gun 3D");
        foreach (var process in processes)
        {
            if (!IsProcessSuspended(process))
            {
                return process;
            }
        }
        return null;
    }

    private static bool IsProcessSuspended(Process process)
    {
        process.Refresh();

        if (process.Threads.Count == 0)
            return false;

        foreach (ProcessThread thread in process.Threads)
        {
            try
            {
                if (thread.WaitReason != ThreadWaitReason.Suspended)
                    return false;
            } catch (Exception ex)
            {
                Logger.ConsoleWrite($"[<WARN>] Skipping thread {thread.Id} for reason: {ex.Message}", ConsoleColor.Yellow);
                return true;
            }
        }

        return true;
    }

    private static void KeepConsoleOpen()
    {
        Console.WriteLine("Press any key to exit... (auto-exiting in 10 seconds)");
        var task = Task.Run(() => Console.ReadKey(intercept: true));
        if (task.Wait(TimeSpan.FromSeconds(10)))
        {
            Console.WriteLine("\nExiting...");
        }
        else
        {
            Console.WriteLine("\nAuto-exiting...");
        }
    }
}
