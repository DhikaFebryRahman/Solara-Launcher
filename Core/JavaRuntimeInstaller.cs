using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Core
{
    public static class JavaRuntimeInstaller
    {
        private const string JavaRuntimeIndexUrl =
            "https://launchermeta.mojang.com/v1/products/java-runtime/2ec0cc96c44e5a76b9c8b7c39df7210883d12871/all.json";

        public static async Task<string> EnsureJavaAsync(string minecraftRoot, string component, CancellationToken ct)
        {
            string osArchRoot = Path.Combine(minecraftRoot, "runtime", component, "windows-x64");
            string runtimeRoot = Path.Combine(osArchRoot, component);
            string javaw = Path.Combine(runtimeRoot, "bin", "javaw.exe");
            string manifestCachePath = Path.Combine(osArchRoot, "manifest.json");

            JavaRuntimeFileManifest? fileManifest =
                await ResolveFileManifestAsync(component, manifestCachePath, ct).ConfigureAwait(false);

            if (fileManifest == null)
            {
                if (File.Exists(javaw))
                {
                    return javaw;
                }
                throw new InstallException(
                    $"Tidak ada koneksi internet dan Java runtime '{component}' belum pernah terpasang.");
            }

            var downloadTasks = new List<DownloadTask>();

            foreach (var (relativePath, entry) in fileManifest.Files)
            {
                if (entry.Type != "file" || entry.Downloads == null)
                {
                    continue;
                }

                string destination = Path.Combine(runtimeRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                downloadTasks.Add(new DownloadTask
                {
                    Url = entry.Downloads.Raw.Url,
                    DestinationPath = destination,
                    ExpectedSha1 = entry.Downloads.Raw.Sha1,
                    ExpectedSize = entry.Downloads.Raw.Size
                });
            }

            await DownloadManager.RunAllAsync(downloadTasks, maxConcurrency: 8, ct).ConfigureAwait(false);

            if (!File.Exists(javaw))
            {
                throw new InstallException("Instalasi Java runtime selesai tetapi javaw.exe tidak ditemukan.");
            }

            return javaw;
        }

        private static async Task<JavaRuntimeFileManifest?> ResolveFileManifestAsync(string component, string manifestCachePath, CancellationToken ct)
        {
            try
            {
                string indexJson = await DownloadManager.GetStringAsync(JavaRuntimeIndexUrl, ct).ConfigureAwait(false);
                var index = JsonSerializer.Deserialize<JavaRuntimeAllJson>(indexJson);

                if (index?.WindowsX64 != null &&
                    index.WindowsX64.TryGetValue(component, out var entries) &&
                    entries.Count > 0)
                {
                    string manifestJson = await DownloadManager.GetStringAsync(entries[0].Manifest.Url, ct).ConfigureAwait(false);

                    string? cacheDir = Path.GetDirectoryName(manifestCachePath);
                    if (!string.IsNullOrEmpty(cacheDir))
                    {
                        Directory.CreateDirectory(cacheDir);
                        await File.WriteAllTextAsync(manifestCachePath, manifestJson, ct).ConfigureAwait(false);
                    }

                    return JsonSerializer.Deserialize<JavaRuntimeFileManifest>(manifestJson);
                }
            }
            catch (Exception)
            {
            }

            if (File.Exists(manifestCachePath))
            {
                try
                {
                    string cachedJson = await File.ReadAllTextAsync(manifestCachePath, ct).ConfigureAwait(false);
                    return JsonSerializer.Deserialize<JavaRuntimeFileManifest>(cachedJson);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }
    }
}
