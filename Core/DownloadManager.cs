using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MinecraftLauncher.Core
{
    public sealed class DownloadTask
    {
        public required string Url { get; init; }
        public required string DestinationPath { get; init; }
        public string ExpectedSha1 { get; init; } = string.Empty;
        public long ExpectedSize { get; init; }
    }

    public static class DownloadManager
    {
        private static readonly HttpClient Http = CreateClient();

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("MinecraftLauncher/1.0");
            return client;
        }

        public static async Task<string> GetStringAsync(string url, CancellationToken ct)
        {
            using var response = await Http.GetAsync(url, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        }

        public static async Task DownloadFileAsync(DownloadTask task, CancellationToken ct)
        {
            if (HashUtil.IsValid(task.DestinationPath, task.ExpectedSha1, task.ExpectedSize))
            {
                return;
            }

            string? directory = Path.GetDirectoryName(task.DestinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            const int maxAttempts = 3;
            Exception? lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    string tempPath = task.DestinationPath + ".part";

                    using (var response = await Http.GetAsync(task.Url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await response.Content.CopyToAsync(fileStream, ct).ConfigureAwait(false);
                    }

                    if (File.Exists(task.DestinationPath))
                    {
                        File.Delete(task.DestinationPath);
                    }
                    File.Move(tempPath, task.DestinationPath);

                    if (!HashUtil.IsValid(task.DestinationPath, task.ExpectedSha1, task.ExpectedSize))
                    {
                        throw new IOException($"Verifikasi SHA-1 gagal untuk: {task.DestinationPath}");
                    }

                    return;
                }
                catch (Exception ex) when (attempt < maxAttempts)
                {
                    lastError = ex;
                    await Task.Delay(500 * attempt, ct).ConfigureAwait(false);
                }
            }

            string leftoverTempPath = task.DestinationPath + ".part";
            if (File.Exists(leftoverTempPath))
            {
                File.Delete(leftoverTempPath);
            }

            throw new IOException($"Gagal mengunduh {task.Url} setelah {maxAttempts} percobaan.", lastError);
        }

        public static async Task RunAllAsync(System.Collections.Generic.IEnumerable<DownloadTask> tasks, int maxConcurrency, CancellationToken ct)
        {
            using var throttle = new SemaphoreSlim(maxConcurrency);
            var running = new System.Collections.Generic.List<Task>();

            foreach (var task in tasks)
            {
                await throttle.WaitAsync(ct).ConfigureAwait(false);
                running.Add(Task.Run(async () =>
                {
                    try
                    {
                        await DownloadFileAsync(task, ct).ConfigureAwait(false);
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }, ct));
            }

            await Task.WhenAll(running).ConfigureAwait(false);
        }
    }
}
