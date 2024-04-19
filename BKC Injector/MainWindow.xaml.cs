using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;

namespace BKC_Injector
{
    public partial class MainWindow : Window
    {
        private const string DLLName = "PixelGunCheat.dll";
        private const string FontName = "UbuntuMono-Regular.ttf";
        private const string ConfigName = "config.ini";
        private static readonly string BaseDirectory = GetApplicationDirectory();
        private static readonly string DependenciesDir = Path.Combine(BaseDirectory, "dependencies");
        private static readonly string DefaultDownloadUrl = $"https://github.com/stanuwu/PixelGunCheatInternal/releases/latest/download/{DLLName}";
        private static readonly HttpClient HttpClient = new();
        private DispatcherTimer? autoInjectTimer;
        private bool isWaitingForProcess = false;
        private bool isUpdateInProgress = false;

        public MainWindow()
        {
            InitializeComponent();

            if (!CheckRequiredDLLs())
            {
                MessageBox.Show("VC Redist is not installed. Please install it from: https://aka.ms/vs/17/release/vc_redist.x64.exe",
                                "BKC Injector", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            Loaded += async (_, __) => await InitializeApplication();
        }

        private static string GetApplicationDirectory() =>
            Path.GetDirectoryName(Process.GetCurrentProcess()?.MainModule?.FileName) ??
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ??
            Environment.CurrentDirectory;

        private static bool CheckRequiredDLLs()
        {
            string systemPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
            string dllPath1 = Path.Combine(systemPath, "VCRUNTIME140.dll");
            string dllPath2 = Path.Combine(systemPath, "MSVCP140.dll");
            return File.Exists(dllPath1) && File.Exists(dllPath2);
        }

        private async Task InitializeApplication()
        {
            CheckAndCreateIniFile();
            bool configUpdated = ValidateAndUpdateConfigFile();
            var settings = ReadIniFileSettings(configUpdated ? "re-read" : "read");

            EnsureDependenciesDirectory();
            await EnsureFontFileExists();
            DisableUI();

            await EnsureDllIsReady(settings.autoUpdate, settings.forcedVersion);

            ApplyConfigurationSettings(settings);
            EnableUI();
        }

        private void SetupAutoInject(bool autoInject)
        {
            if (!autoInject) return;

            autoInjectTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };
            autoInjectTimer.Tick += AutoInjectTimer_Tick;
            autoInjectTimer.Start();
            InjectButton.IsEnabled = false;
            InjectButton.Opacity = 0.5;
            InjectButton.Cursor = Cursors.No;
            AppendStatus("Auto-inject is enabled. Waiting for Pixel Gun 3D to open...");
        }

        private void AutoInjectTimer_Tick(object? sender, EventArgs e)
        {
            if (isUpdateInProgress) return;
            var process = GetFirstNonSuspendedPixelGun3DInstance();
            if (process != null)
            {
                if (isWaitingForProcess)
                {
                    AppendStatus("Pixel Gun 3D instance found. Auto-injecting...");
                    isWaitingForProcess = false;
                }
                Inject();
                autoInjectTimer?.Stop();
            }
            else if (!isWaitingForProcess)
            {
                AppendStatus("Waiting for a Pixel Gun 3D process...");
                isWaitingForProcess = true;
            }
        }

        private void DisableUI()
        {
            InjectButton.IsEnabled = false;
            isUpdateInProgress = true;
        }

        private void EnableUI()
        {
            if (!isUpdateInProgress)
            {
                InjectButton.IsEnabled = true;
                InjectButton.Opacity = 1;
                InjectButton.Cursor = Cursors.Arrow;
            }
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
                AppendStatus("Configuration file missing; creating a new one with default settings.");
                CreateDefaultConfigFile(iniPath);
            }
        }

        private static void CreateDefaultConfigFile(string iniPath)
        {
            string[] iniContent = [
                "[BKC Configuration]",
                "AutoUpdate = true",
                "ForceVersion = ",
                "AutoInject = false"
            ];
            File.WriteAllLines(iniPath, iniContent);
        }

        private bool ValidateAndUpdateConfigFile()
        {
            string iniPath = Path.Combine(BaseDirectory, ConfigName);
            var parser = new IniParser(iniPath);
            bool isUpdated = false;

            if (string.IsNullOrWhiteSpace(parser.GetValue("BKC Configuration", "AutoUpdate")))
            {
                parser.SetValue("BKC Configuration", "AutoUpdate", "true");
                isUpdated = true;
            }

            if (string.IsNullOrWhiteSpace(parser.GetValue("BKC Configuration", "AutoInject")))
            {
                parser.SetValue("BKC Configuration", "AutoInject", "false");
                isUpdated = true;
            }

            string forceVersion = parser.GetValue("BKC Configuration", "ForceVersion") ?? "";
            if (string.IsNullOrEmpty(forceVersion) || !VersionRegex().IsMatch(forceVersion))
            {
                parser.SetValue("BKC Configuration", "ForceVersion", "");
                isUpdated = true;
            }

            if (isUpdated)
            {
                parser.SaveSettings(iniPath);
                AppendStatus("Configuration file updated with default settings due to missing or invalid entries.");
                ReloadConfiguration();
            }

            return isUpdated;
        }

        private void ReloadConfiguration()
        {
            var settings = ReadIniFileSettings(null);
            ApplyConfigurationSettings(settings);
        }

        private void ApplyConfigurationSettings((bool autoUpdate, string forcedVersion, bool autoInject) settings)
        {
            if (settings.autoInject)
            {
                SetupAutoInject(true);
            }
            else
            {
                if (autoInjectTimer != null)
                {
                    autoInjectTimer.Stop();
                    autoInjectTimer = null;
                }
                InjectButton.IsEnabled = true;
                InjectButton.Opacity = 1;
                InjectButton.Cursor = Cursors.Arrow;
            }
        }

        private (bool autoUpdate, string forcedVersion, bool autoInject) ReadIniFileSettings(string? context)
        {
            string iniPath = Path.Combine(BaseDirectory, ConfigName);
            try
            {
                var parser = new IniParser(iniPath);
                bool autoUpdate = TryGetBoolValue(parser, "BKC Configuration", "AutoUpdate", true);
                string forcedVersion = parser.GetValue("BKC Configuration", "ForceVersion")?.Trim() ?? "";
                bool autoInject = TryGetBoolValue(parser, "BKC Configuration", "AutoInject", false);

                if (!string.IsNullOrEmpty(context))
                    AppendStatus($"Configuration file was {context} successfully");

                return (autoUpdate, forcedVersion, autoInject);
            }
            catch (Exception ex)
            {
                AppendStatus($"Failed to {context ?? "read"} configuration file: {ex.Message}");
                return (true, string.Empty, false);
            }
        }

        private static bool TryGetBoolValue(IniParser parser, string section, string key, bool defaultValue)
        {
            string? value = parser.GetValue(section, key);
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            return defaultValue;
        }

        private async Task EnsureDllIsReady(bool autoUpdate, string forcedVersion)
        {
            string downloadUrl = GetDownloadUrl(forcedVersion);
            string dllPath = Path.Combine(DependenciesDir, DLLName);

            bool exists = File.Exists(dllPath);
            if (!exists || (autoUpdate && await IsUpdateNeeded(dllPath, downloadUrl)))
            {
                DisableUI();
                try
                {
                    if (await TryDownloadDll(dllPath, downloadUrl))
                    {
                        AppendStatus("DLL downloaded and updated successfully.");
                    }
                    else
                    {
                        AppendStatus("Failed to download or update the DLL.");
                        throw new InvalidOperationException("DLL update failed, cannot proceed with auto-inject.");
                    }
                }
                finally
                {
                    isUpdateInProgress = false;
                    EnableUI();
                }
            }
            else
            {
                AppendStatus("DLL is up to date. No update needed.");
            }
        }

        private string GetDownloadUrl(string forcedVersion)
        {
            if (!string.IsNullOrEmpty(forcedVersion) && VersionRegex().IsMatch(forcedVersion))
            {
                AppendStatus($"Forcing version: {forcedVersion}");
                return $"https://github.com/stanuwu/PixelGunCheatInternal/releases/download/{forcedVersion}/{DLLName}";
            }
            AppendStatus("Using default download URL.");
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

        private async Task EnsureFontFileExists()
        {
            string fontPath = Path.Combine(DependenciesDir, FontName);
            string fontUrl = $"https://boykissers.dev/DuPONT/dependencies/raw/branch/main/{FontName}";

            if (!File.Exists(fontPath))
            {
                AppendStatus("Font file missing; downloading...");
                try
                {
                    byte[] fontData = await HttpClient.GetByteArrayAsync(fontUrl);
                    File.WriteAllBytes(fontPath, fontData);
                    AppendStatus("Font file downloaded successfully.");
                }
                catch (Exception ex)
                {
                    AppendStatus($"Failed to download font file: {ex.Message}");
                }
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
            if (!File.Exists(dllPath))
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
