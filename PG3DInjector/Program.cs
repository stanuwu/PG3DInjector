using System.Diagnostics;
using System.Threading.Tasks;
using PG3DInjector;
using PG3DInjector.Modules;

internal class Program
{
    private static readonly string Version = "v1.4-3";
    private static readonly string DllName = "PixelGunCheat.dll";
    private static readonly string DownloadUrl = 
        $"https://github.com/stanuwu/PixelGunCheatInternal/releases/latest/download/{DllName}";

    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="_">Command-line arguments (unused).</param>
    private static async Task Main(string[] _)
    {
        Console.Title = $"BKC Injector {Version}";

        var client = HttpClientModule.InitializeHttpClient();

        var targetProcess = GetFirstNonSuspendedPixelGun3DInstance();
        if (targetProcess == null)
        {
            Logger.Error("Please start a non-suspended instance of the game before running the injector.");
            KeepConsoleOpen();
            return;
        }

        if (!LocalDllExists(DllName) && !await HttpClientModule.TryDownloadDll(client, DownloadUrl, DllName))
        {
            KeepConsoleOpen();
            return;
        }

        InjectDll(DllName, targetProcess);
        KeepConsoleOpen();
    }

    /// <summary>
    /// Checks if a local copy of the specified DLL exists.
    /// </summary>
    /// <param name="dllName">The name of the DLL to check.</param>
    /// <returns>True if the DLL exists locally, false otherwise.</returns>
    private static bool LocalDllExists(string dllName)
    {
        var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
        if (File.Exists(dllPath)) return true;

        Logger.Log($"Injection failed: {dllName} was not found locally, attempting download.");
        return false;
    }

    /// <summary>
    /// Injects the specified DLL into a target process.
    /// </summary>
    /// <param name="dllName">The name of the DLL to inject.</param>
    /// <param name="targetProcess">The process into which the DLL should be injected.</param>
    private static void InjectDll(string dllName, Process targetProcess)
    {
        var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
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

    /// <summary>
    /// Gets the first non-suspended instance of the Pixel Gun 3D process.
    /// </summary>
    /// <returns>The first non-suspended instance of the Pixel Gun 3D process, or null if none is found.</returns>
    private static Process? GetFirstNonSuspendedPixelGun3DInstance()
    {
        return Process.GetProcessesByName("Pixel Gun 3D")
            .FirstOrDefault(p => !IsProcessSuspended(p));
    }

    /// <summary>
    /// Checks if a process is suspended.
    /// </summary>
    /// <param name="process">The process to check.</param>
    /// <returns>True if the process is suspended, false otherwise.</returns>
    private static bool IsProcessSuspended(Process process)
    {
        process.Refresh();
        return process.Threads.Cast<ProcessThread>()
            .All(t => t.ThreadState == System.Diagnostics.ThreadState.Wait &&
                      t.WaitReason == ThreadWaitReason.Suspended);
    }

    /// <summary>
    /// Keeps the console open until a key is pressed or 10 seconds have passed.
    /// </summary>
    private static void KeepConsoleOpen()
    {
        Console.WriteLine("Press any key to exit... (auto-exiting in 10 seconds)");
        if (Task.Run(() => Console.ReadKey(true)).Wait(TimeSpan.FromSeconds(10)))
            Console.WriteLine("\nExiting...");
        else
            Console.WriteLine("\nAuto-exiting...");
    }
}
