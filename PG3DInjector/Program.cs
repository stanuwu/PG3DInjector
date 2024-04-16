using System.Diagnostics;
using PG3DInjector;

internal class Program
{
    private static readonly string Version = "v1.4-3";
    private static readonly string DLLName = "PixelGunCheat.dll";
    private static readonly string DownloadUrl = $"https://github.com/stanuwu/PixelGunCheatInternal/releases/latest/download/{DLLName}";

    private static async Task Main(string[] _)
    {
        Console.Title = $"BKC Injector {Version}";

        var client = InitializeHttpClient();

        var targetProcess = GetFirstNonSuspendedPixelGun3DInstance();
        if (targetProcess == null)
        {
            Logger.Error("Please start a non-suspended instance of the game before running the injector.");
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

        Logger.Log($"Injection failed: {dllName} was not found locally, attempting download.");
        return false;
    }

    private static async Task<bool> TryDownloadDll(HttpClient client)
    {
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DLLName);
        try
        {
            await DownloadFile(client, DownloadUrl, dllPath);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Injection failed: {ex.Message}");
            return false;
        }
    }

    private static async Task DownloadFile(HttpClient client, string url, string outputPath)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        byte[] fileData = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(outputPath, fileData);
        Logger.Okay($"{Path.GetFileName(outputPath)} downloaded successfully.");
    }

    private static void InjectDll(string dllName, Process targetProcess)
    {
        string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        Logger.Log($"Injecting {dllName} into process {targetProcess.Id}.");

        try
        {
            Injector.Map(targetProcess.ProcessName, dllPath);
            Logger.Okay("Injection completed successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Injection failed: {ex.Message}");
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
