using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;

namespace BKC_Injector
{
    public partial class MainWindow : Window
    {
        private const string DLLName = "PixelGunCheat.dll";
        private const string ConfigName = "config.ini";
        private static readonly string BaseDirectory = GetApplicationDirectory();
        private static readonly string DependenciesDir = Path.Combine(BaseDirectory, "dependencies");
        private static readonly string DefaultDownloadUrl = $"https://github.com/stanuwu/PixelGunCheatInternal/releases/latest/download/{DLLName}";
        private static readonly HttpClient HttpClient = new();

        public MainWindow()
        {
            InitializeComponent();
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            Loaded += async (_, __) => await InitializeApplication();
        }

        private static string GetApplicationDirectory()
        {
            string? directory = Path.GetDirectoryName(Process.GetCurrentProcess()?.MainModule?.FileName) ??
                                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
                                Environment.CurrentDirectory;
            return directory;
        }

        private async Task InitializeApplication()
        {
            CheckAndCreateIniFile();
            EnsureDependenciesDirectory();
            DisableUI();
            AppendStatus("Checking DLL status...");
            await EnsureDllIsReady();
            EnableUI();
        }

        private void DisableUI()
        {
            InjectButton.IsEnabled = false;
        }

        private void EnableUI()
        {
            InjectButton.IsEnabled = true;
        }

        private void EnsureDependenciesDirectory()
        {
            if (!Directory.Exists(DependenciesDir))
            {
                Directory.CreateDirectory(DependenciesDir);
                AppendStatus("Dependencies directory created.");
            }
        }

        private void CheckAndCreateIniFile()
        {
            string iniPath = Path.Combine(BaseDirectory, ConfigName);
            if (!File.Exists(iniPath))
            {
                AppendStatus("Creating INI file...");
                string[] iniContent = [
                    "[BKC Configuration]",
                    "AutoUpdate = true",
                    "ForceVersion = ",
                    "; Do not mess with this file unless you know what you are doing. Here be dragons!",
                    "; Forced version must be in format 'vX.X-x' (e.g., v1.5, v1.5-2) and must be a version equal to or newer than v1.4"
                ];
                File.WriteAllLines(iniPath, iniContent);
            }
        }

        private (bool autoUpdate, string forcedVersion) ReadIniFileSettings()
        {
            string iniPath = Path.Combine(BaseDirectory, ConfigName);
            try
            {
                var parser = new IniParser(iniPath);
                string autoUpdateStr = parser.GetValue("BKC Configuration", "AutoUpdate");
                string forcedVersion = parser.GetValue("BKC Configuration", "ForceVersion").Trim();

                bool autoUpdate = bool.Parse(autoUpdateStr);

                AppendStatus("Configuration file read successfully.");
                return (autoUpdate, forcedVersion);
            }
            catch (Exception ex)
            {
                AppendStatus($"Failed to read configuration file: {ex.Message}");
                return (true, string.Empty);
            }
        }


        private async Task EnsureDllIsReady()
        {
            var (autoUpdate, forcedVersion) = ReadIniFileSettings();
            string downloadUrl = GetDownloadUrl(forcedVersion);
            string dllPath = Path.Combine(DependenciesDir, DLLName);

            bool exists = File.Exists(dllPath);
            if (!exists || (autoUpdate && await IsUpdateNeeded(dllPath, downloadUrl)))
            {
                if (!exists)
                {
                    AppendStatus($"DLL not found. Initiating download for {Path.GetFileName(downloadUrl)}...");
                }
                else if (autoUpdate)
                {
                    AppendStatus($"Update available. Initiating update for {Path.GetFileName(downloadUrl)}...");
                }

                if (await TryDownloadDll(dllPath, downloadUrl))
                {
                    if (!exists)
                    {
                        AppendStatus("DLL downloaded successfully.");
                    }
                    else
                    {
                        AppendStatus("DLL updated successfully.");
                    }
                }
                else
                {
                    AppendStatus("Failed to download or update the DLL.");
                }
            }
            else
            {
                if (!autoUpdate)
                {
                    AppendStatus("Auto-update is disabled. Skipped version check.");
                }
                else
                {
                    AppendStatus("The DLL is up to date. No updates needed.");
                }
            }
        }

        private string GetDownloadUrl(string forcedVersion)
        {
            if (!string.IsNullOrEmpty(forcedVersion))
            {
                if (VersionRegex().IsMatch(forcedVersion))
                {
                    AppendStatus($"Forcing version: {forcedVersion}");
                    return $"https://github.com/stanuwu/PixelGunCheatInternal/releases/download/{forcedVersion}/{DLLName}";
                }
                else
                {
                    AppendStatus($"Invalid version format '{forcedVersion}'. Falling back to latest.");
                }
            }
            return DefaultDownloadUrl;
        }

        private async Task<bool> TryDownloadDll(string path, string url)
        {
            try
            {
                byte[] fileData = await HttpClient.GetByteArrayAsync(url);
                File.WriteAllBytes(path, fileData);
                AppendStatus($"Downloaded {Path.GetFileName(url)} successfully.");
                return true;
            }
            catch (Exception ex)
            {
                AppendStatus($"Unable to find or download {Path.GetFileName(url)}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> IsUpdateNeeded(string localPath, string url)
        {
            try
            {
                byte[] localHash = ComputeFileHash(localPath);
                byte[] remoteHash = await FetchRemoteFileHash(url);
                return !localHash.SequenceEqual(remoteHash);
            }
            catch (Exception ex)
            {
                AppendStatus($"Error checking for DLL updates: {ex.Message}");
                return false;
            }
        }

        private static byte[] ComputeFileHash(string filePath)
        {
            using var sha256 = SHA256.Create();
            using var stream = File.OpenRead(filePath);
            return sha256.ComputeHash(stream);
        }

        private async Task<byte[]> FetchRemoteFileHash(string url)
        {
            try
            {
                byte[] fileData = await HttpClient.GetByteArrayAsync(url);
                return SHA256.HashData(fileData);
            }
            catch (Exception ex)
            {
                AppendStatus($"Error fetching remote file hash: {ex.Message}");
                throw;
            }
        }

        private void InjectButton_Click(object sender, RoutedEventArgs e)
        {
            Inject();
        }

        private async void Inject()
        {
            DisableUI();
            AppendStatus("Preparing to inject...");

            var targetProcess = GetFirstNonSuspendedPixelGun3DInstance();
            if (targetProcess == null)
            {
                AppendStatus("No suitable Pixel Gun 3D instance is currently running.");
                EnableUI();
                return;
            }

            string dllPath = Path.Combine(DependenciesDir, DLLName);
            if (!File.Exists(dllPath) || await IsUpdateNeeded(dllPath, DefaultDownloadUrl))
            {
                if (!await TryDownloadDll(dllPath, DefaultDownloadUrl))
                {
                    EnableUI();
                    return;
                }
            }

            Environment.SetEnvironmentVariable("BKC_PATH", BaseDirectory);

            InjectDll(dllPath, targetProcess);
            EnableUI();
        }

        private static Process? GetFirstNonSuspendedPixelGun3DInstance()
        {
            return Process.GetProcessesByName("Pixel Gun 3D").FirstOrDefault(p => !IsProcessSuspended(p));
        }

        private static bool IsProcessSuspended(Process process)
        {
            process.Refresh();
            return process.Threads.Cast<ProcessThread>().All(t => t.ThreadState == System.Diagnostics.ThreadState.Wait && t.WaitReason == ThreadWaitReason.Suspended);
        }

        private void InjectDll(string dllPath, Process process)
        {
            AppendStatus($"Attempting to inject DLL into process ID {process.Id}...");
            try
            {
                Injector.Inject(process, dllPath, AppendStatus);
                AppendStatus("Injection successfully completed.");
            }
            catch (Exception ex)
            {
                Environment.SetEnvironmentVariable("BKC_PATH", null);
                AppendStatus($"Injection failed: {ex.Message}");
            }
        }

        private void AppendStatus(string text)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBox.AppendText($"{text}\n");
                StatusTextBox.ScrollToEnd();
            });
        }

        [GeneratedRegex(@"^v\d+\.\d+(-\d+)?$")]
        private static partial Regex VersionRegex();
    }
}