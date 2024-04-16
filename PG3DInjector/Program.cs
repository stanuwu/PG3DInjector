using System.Diagnostics;
using Newtonsoft.Json.Linq;
using PG3DInjector;

internal class Program
{
    private static readonly string DLLName = "PixelGunCheat.dll";
    private static readonly string LatestReleaseApiUrl = "https://api.github.com/repos/stanuwu/PixelGunCheatInternal/releases/latest";

    private static async Task Main(string[] _)
    {
        var client = InitializeHttpClient();

        var targetProcess = GetFirstNonSuspendedPixelGun3DInstance();
        if (targetProcess == null)
        {
            Logger.ConsoleWrite("[<ERROR>] Please start a non-suspended instance of the game before running the injector.", ConsoleColor.Red);
            KeepConsoleOpen();
            return;
        }

        if (!LocalDllExists(DLLName) && !await TryDownloadDll(client))
        {
            KeepConsoleOpen();
            return;
        }

        InjectDll(DLLName, targetProcess);
        KeepConsoleOpen();
    }

    private static HttpClient InitializeHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        return client;
    }

    private static bool LocalDllExists(string dllName)
    {
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        if (File.Exists(dllPath))
        {
            return true;
        }

        Logger.ConsoleWrite($"[<WARN>] {dllName} was not found locally, attempting download.", ConsoleColor.Yellow);
        return false;
    }

    private static async Task<bool> TryDownloadDll(HttpClient client)
    {
        try
        {
            string json = await GetLatestReleaseJson(client);
            JObject releaseInfo = JObject.Parse(json);
            return await DownloadDllIfAvailable(client, releaseInfo);
        }
        catch (Exception ex)
        {
            Logger.ConsoleWrite($"[<ERROR>] Failed to download the latest DLL: {ex.Message}", ConsoleColor.Red);
            return false;
        }
    }

    private static async Task<string> GetLatestReleaseJson(HttpClient client)
    {
        HttpResponseMessage response = await client.GetAsync(LatestReleaseApiUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<bool> DownloadDllIfAvailable(HttpClient client, JObject releaseInfo)
    {
        if (releaseInfo["assets"] is JArray assets)
        {
            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DLLName);
            string? downloadUrl = assets.Children<JObject>()
                .FirstOrDefault(a => a["name"]?.ToString().Equals(DLLName, StringComparison.OrdinalIgnoreCase) == true)?
                .Value<string>("browser_download_url");

            if (!string.IsNullOrEmpty(downloadUrl))
            {
                await DownloadFile(client, downloadUrl, dllPath);
                return true;
            }
        }

        Logger.ConsoleWrite("[<ERROR>] No assets found in the latest release.", ConsoleColor.Red);
        return false;
    }

    private static async Task DownloadFile(HttpClient client, string url, string outputPath)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        byte[] fileData = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(outputPath, fileData);
        Logger.ConsoleWrite($"[<OKAY>] {Path.GetFileName(outputPath)} downloaded successfully.", ConsoleColor.Green);
    }

    private static void InjectDll(string dllName, Process targetProcess)
    {
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        Logger.ConsoleWrite($"[<INFO>] Injecting {dllName} into process {targetProcess.Id}.", ConsoleColor.Cyan);

        try
        {
            Injector.Map(targetProcess.ProcessName, dllPath);
            Logger.ConsoleWrite("[<OKAY>] Injection completed successfully.", ConsoleColor.Green);
        }
        catch (Exception ex)
        {
            Logger.ConsoleWrite($"[<ERROR>] Injection failed: {ex.Message}", ConsoleColor.Red);
        }
    }

    private static Process? GetFirstNonSuspendedPixelGun3DInstance()
    {
        return Process.GetProcessesByName("Pixel Gun 3D")
            .FirstOrDefault(p => !IsProcessSuspended(p));
    }

    private static bool IsProcessSuspended(Process process)
    {
        process.Refresh();
        return process.Threads.Cast<ProcessThread>()
            .All(t => t.ThreadState == System.Diagnostics.ThreadState.Wait && t.WaitReason == ThreadWaitReason.Suspended);
    }

    private static void KeepConsoleOpen()
    {
        Console.WriteLine("Press any key to exit... (auto-exiting in 10 seconds)");
        if (Task.Run(() => Console.ReadKey(true)).Wait(TimeSpan.FromSeconds(10)))
        {
            Console.WriteLine("\nExiting...");
        }
        else
        {
            Console.WriteLine("\nAuto-exiting...");
        }
    }
}
