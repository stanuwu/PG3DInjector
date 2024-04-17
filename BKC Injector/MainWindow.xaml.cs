using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Windows;

namespace BKC_Injector
{
    public partial class MainWindow : Window
    {
        private const string DLLName = "PixelGunCheat.dll";
        private static readonly string BaseDirectory = GetApplicationDirectory();
        private static readonly string DownloadUrl = $"https://github.com/stanuwu/PixelGunCheatInternal/releases/latest/download/{DLLName}";
        private static readonly HttpClient HttpClient = new();

        public MainWindow()
        {
            InitializeComponent();
            HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            Loaded += async (_, __) => await InitializeApplication();
        }

        private static string GetApplicationDirectory()
        {
            string? directory = Path.GetDirectoryName(Process.GetCurrentProcess()?.MainModule?.FileName);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                return directory;

            directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                return directory;

            return Environment.CurrentDirectory;
        }

        private async Task InitializeApplication()
        {
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

        private async Task EnsureDllIsReady()
        {
            string dllPath = Path.Combine(BaseDirectory, DLLName);

            if (!File.Exists(dllPath))
            {
                AppendStatus("The required DLL is missing, initiating download...");
                if (await TryDownloadDll(dllPath))
                {
                    AppendStatus("DLL has been successfully downloaded.");
                }
                else
                {
                    AppendStatus("Failed to download the required DLL.");
                }
            }
            else if (await IsUpdateNeeded(dllPath))
            {
                AppendStatus("An update for the DLL is available, updating now...");
                if (await TryDownloadDll(dllPath))
                {
                    AppendStatus("The DLL has been successfully updated.");
                }
                else
                {
                    AppendStatus("Failed to update the DLL.");
                }
            }
            else
            {
                AppendStatus("The DLL is up to date. No updates needed.");
            }
        }

        private async Task<bool> TryDownloadDll(string path)
        {
            try
            {
                byte[] fileData = await HttpClient.GetByteArrayAsync(DownloadUrl);
                File.WriteAllBytes(path, fileData);
                return true;
            }
            catch (HttpRequestException ex)
            {
                AppendStatus($"Network error during DLL download: {ex.Message}");
                return false;
            }
            catch (IOException ex)
            {
                AppendStatus($"File system error during DLL download: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                AppendStatus($"Unexpected error during DLL download: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> IsUpdateNeeded(string localPath)
        {
            try
            {
                byte[] localHash = ComputeFileHash(localPath);
                byte[] remoteHash = await FetchRemoteFileHash();
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

        private async Task<byte[]> FetchRemoteFileHash()
        {
            try
            {
                byte[] fileData = await HttpClient.GetByteArrayAsync(DownloadUrl);
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

            string dllPath = Path.Combine(BaseDirectory, DLLName);
            if (!File.Exists(dllPath) || await IsUpdateNeeded(dllPath))
            {
                if (!await TryDownloadDll(dllPath))
                {
                    EnableUI();
                    return;
                }
            }

            InjectDll(dllPath, targetProcess);
            EnableUI();
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
    }
}