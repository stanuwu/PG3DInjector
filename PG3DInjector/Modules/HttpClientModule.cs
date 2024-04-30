using System.Net.Http;
using System.IO;
using System.Threading.Tasks;

namespace PG3DInjector.Modules
{
    /// <summary>
    /// A module for handling HTTP-related functions.
    /// </summary>
    public static class HttpClientModule
    {
        /// <summary>
        /// Initializes an HTTP client with default settings.
        /// </summary>
        /// <returns>An initialized HTTP client.</returns>
        public static HttpClient InitializeHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            return client;
        }

        /// <summary>
        /// Attempts to download the specified DLL.
        /// </summary>
        /// <param name="client">The HTTP client to use for the download.</param>
        /// <param name="url">The URL to download the DLL from.</param>
        /// <param name="dllName">The name of the DLL to download.</param>
        /// <returns>True if the DLL is successfully downloaded, false otherwise.</returns>
        public static async Task<bool> TryDownloadDll(HttpClient client, string url, string dllName)
        {
            var dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dllName);
            try
            {
                await DownloadFile(client, url, dllPath);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Download failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Downloads a file from the specified URL and saves it to the specified path.
        /// </summary>
        /// <param name="client">The HTTP client to use for the download.</param>
        /// <param name="url">The URL to download from.</param>
        /// <param name="outputPath">The path to save the downloaded file.</param>
        private static async Task DownloadFile(HttpClient client, string url, string outputPath)
        {
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var fileData = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(outputPath, fileData);
            Logger.Okay($"{Path.GetFileName(outputPath)} downloaded successfully.");
        }
    }
}
