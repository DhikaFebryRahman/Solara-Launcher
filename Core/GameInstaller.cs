using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Core
{
    public sealed class InstallException : Exception
    {
        public InstallException(string message, Exception? inner = null) : base(message, inner) { }
    }

    public sealed class VersionNotFoundException : Exception
    {
        public VersionNotFoundException(string message) : base(message) { }
    }

    public static class GameInstaller
    {
        public static string GetGameDirectory()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, ".minecraft");
        }

        public static async Task<InstallResult> InstallAsync(string playerVersion, string gameDirectory, IProgress<string> progress, CancellationToken ct)
        {
            string gameDir = gameDirectory;
            Directory.CreateDirectory(gameDir);

            VersionManifestEntry entry = await ResolveManifestEntryAsync(gameDir, playerVersion, progress, ct).ConfigureAwait(false);

            progress.Report("Menyiapkan berkas versi...");
            string versionDir = Path.Combine(gameDir, "versions", playerVersion);
            string versionJsonPath = Path.Combine(versionDir, $"{playerVersion}.json");

            if (!string.IsNullOrEmpty(entry.Url))
            {
                await DownloadManager.DownloadFileAsync(new DownloadTask
                {
                    Url = entry.Url,
                    DestinationPath = versionJsonPath,
                    ExpectedSha1 = entry.Sha1
                }, ct).ConfigureAwait(false);
            }
            else if (!File.Exists(versionJsonPath))
            {
                throw new VersionNotFoundException($"Versi '{playerVersion}' tidak ditemukan dan tidak ada berkas lokal untuk dipakai secara offline.");
            }

            var versionDetail = JsonSerializer.Deserialize<VersionDetail>(await File.ReadAllTextAsync(versionJsonPath, ct).ConfigureAwait(false))
                ?? throw new InstallException("Berkas versi rusak atau tidak valid.");

            progress.Report("Memverifikasi client.jar...");
            string clientJarPath = Path.Combine(versionDir, $"{playerVersion}.jar");
            await DownloadManager.DownloadFileAsync(new DownloadTask
            {
                Url = versionDetail.Downloads.Client.Url,
                DestinationPath = clientJarPath,
                ExpectedSha1 = versionDetail.Downloads.Client.Sha1,
                ExpectedSize = versionDetail.Downloads.Client.Size
            }, ct).ConfigureAwait(false);

            progress.Report("Memverifikasi libraries...");
            var (classpathEntries, nativesJars) = await InstallLibrariesAsync(gameDir, versionDetail, ct).ConfigureAwait(false);
            classpathEntries.Add(clientJarPath);

            progress.Report("Menyiapkan native libraries...");
            string nativesDir = Path.Combine(versionDir, "natives");
            ExtractNatives(nativesDir, nativesJars);

            progress.Report("Memverifikasi assets...");
            await InstallAssetsAsync(gameDir, versionDetail, ct).ConfigureAwait(false);

            string? loggingArgTemplate = null;
            string? loggingFilePath = null;
            if (versionDetail.Logging?.Client != null)
            {
                progress.Report("Memverifikasi konfigurasi logging...");
                var logCfg = versionDetail.Logging.Client;
                string fileName = Path.GetFileName(logCfg.File.Path ?? logCfg.File.Url.Split('/').Last());
                loggingFilePath = Path.Combine(gameDir, "assets", "log_configs", fileName);
                await DownloadManager.DownloadFileAsync(new DownloadTask
                {
                    Url = logCfg.File.Url,
                    DestinationPath = loggingFilePath,
                    ExpectedSha1 = logCfg.File.Sha1,
                    ExpectedSize = logCfg.File.Size
                }, ct).ConfigureAwait(false);
                loggingArgTemplate = logCfg.Argument;
            }

            progress.Report("Menyiapkan Java runtime...");
            string javaExe = await JavaRuntimeInstaller.EnsureJavaAsync(gameDir, versionDetail.JavaVersion.Component, ct).ConfigureAwait(false);

            return new InstallResult
            {
                GameDirectory = gameDir,
                AssetsRoot = Path.Combine(gameDir, "assets"),
                NativesDirectory = nativesDir,
                JavaExecutablePath = javaExe,
                VersionDetail = versionDetail,
                ClasspathEntries = classpathEntries,
                LoggingArgumentTemplate = loggingArgTemplate,
                LoggingConfigFilePath = loggingFilePath
            };
        }

        private static async Task<VersionManifestEntry> ResolveManifestEntryAsync(string gameDir, string playerVersion, IProgress<string> progress, CancellationToken ct)
        {
            const string manifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
            string manifestCachePath = Path.Combine(gameDir, "version_manifest_v2.json");

            VersionManifestRoot? manifest = null;

            try
            {
                progress.Report("Mengambil daftar versi resmi...");
                string json = await DownloadManager.GetStringAsync(manifestUrl, ct).ConfigureAwait(false);
                manifest = JsonSerializer.Deserialize<VersionManifestRoot>(json);
                if (manifest != null)
                {
                    await File.WriteAllTextAsync(manifestCachePath, json, ct).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                progress.Report("Tidak ada koneksi internet, mencoba menggunakan data lokal...");
                if (File.Exists(manifestCachePath))
                {
                    try
                    {
                        manifest = JsonSerializer.Deserialize<VersionManifestRoot>(await File.ReadAllTextAsync(manifestCachePath, ct).ConfigureAwait(false));
                    }
                    catch (Exception)
                    {
                        manifest = null;
                    }
                }
            }

            var found = manifest?.Versions.FirstOrDefault(v => string.Equals(v.Id, playerVersion, StringComparison.Ordinal));
            if (found != null)
            {
                return found;
            }

            return new VersionManifestEntry { Id = playerVersion, Url = string.Empty, Sha1 = string.Empty };
        }

        private static async Task<(List<string> classpath, List<string> nativesJars)> InstallLibrariesAsync(string gameDir, VersionDetail versionDetail, CancellationToken ct)
        {
            string librariesRoot = Path.Combine(gameDir, "libraries");
            var classpath = new List<string>();
            var nativesJarPaths = new List<string>();
            var downloadTasks = new List<DownloadTask>();
            var nativesToDownload = new List<(DownloadTask task, string destJar)>();

            foreach (var library in versionDetail.Libraries)
            {
                if (!RuleEvaluator.IsAllowed(library.Rules))
                {
                    continue;
                }

                var artifact = library.Downloads?.Artifact;
                if (artifact != null && !string.IsNullOrEmpty(artifact.Url))
                {
                    string relativePath = artifact.Path ?? artifact.Url[(artifact.Url.IndexOf("/libraries/", StringComparison.Ordinal) + 1)..];
                    string destination = Path.Combine(librariesRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                    downloadTasks.Add(new DownloadTask
                    {
                        Url = artifact.Url,
                        DestinationPath = destination,
                        ExpectedSha1 = artifact.Sha1,
                        ExpectedSize = artifact.Size
                    });
                    classpath.Add(destination);
                }

                if (library.Natives != null && library.Natives.TryGetValue("windows", out string? classifierTemplate))
                {
                    string classifierKey = classifierTemplate.Replace("${arch}", "64");
                    if (library.Downloads?.Classifiers != null &&
                        library.Downloads.Classifiers.TryGetValue(classifierKey, out var nativeArtifact) &&
                        !string.IsNullOrEmpty(nativeArtifact.Url))
                    {
                        string relativePath = nativeArtifact.Path ?? $"{library.Name.Replace(':', '/')}-{classifierKey}.jar";
                        string destination = Path.Combine(librariesRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
                        var task = new DownloadTask
                        {
                            Url = nativeArtifact.Url,
                            DestinationPath = destination,
                            ExpectedSha1 = nativeArtifact.Sha1,
                            ExpectedSize = nativeArtifact.Size
                        };
                        nativesToDownload.Add((task, destination));
                    }
                }
            }

            await DownloadManager.RunAllAsync(downloadTasks, maxConcurrency: 8, ct).ConfigureAwait(false);
            await DownloadManager.RunAllAsync(nativesToDownload.Select(n => n.task), maxConcurrency: 8, ct).ConfigureAwait(false);

            nativesJarPaths.AddRange(nativesToDownload.Select(n => n.destJar));

            return (classpath, nativesJarPaths);
        }

        private static void ExtractNatives(string nativesDir, List<string> nativesJars)
        {
            if (Directory.Exists(nativesDir))
            {
                Directory.Delete(nativesDir, recursive: true);
            }
            Directory.CreateDirectory(nativesDir);

            foreach (string jarPath in nativesJars)
            {
                using var archive = ZipFile.OpenRead(jarPath);
                foreach (var zipEntry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(zipEntry.Name))
                    {
                        continue;
                    }
                    if (zipEntry.FullName.StartsWith("META-INF", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string destPath = Path.Combine(nativesDir, zipEntry.FullName.Replace('/', Path.DirectorySeparatorChar));
                    string? destDir = Path.GetDirectoryName(destPath);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }
                    zipEntry.ExtractToFile(destPath, overwrite: true);
                }
            }
        }

        private static async Task InstallAssetsAsync(string gameDir, VersionDetail versionDetail, CancellationToken ct)
        {
            string indexPath = Path.Combine(gameDir, "assets", "indexes", $"{versionDetail.AssetIndex.Id}.json");
            await DownloadManager.DownloadFileAsync(new DownloadTask
            {
                Url = versionDetail.AssetIndex.Url,
                DestinationPath = indexPath,
                ExpectedSha1 = versionDetail.AssetIndex.Sha1,
                ExpectedSize = versionDetail.AssetIndex.Size
            }, ct).ConfigureAwait(false);

            var assetIndex = JsonSerializer.Deserialize<AssetIndexFile>(await File.ReadAllTextAsync(indexPath, ct).ConfigureAwait(false))
                ?? throw new InstallException("Berkas asset index rusak atau tidak valid.");

            string objectsRoot = Path.Combine(gameDir, "assets", "objects");
            var downloadTasks = new List<DownloadTask>(assetIndex.Objects.Count);

            foreach (var obj in assetIndex.Objects.Values)
            {
                string hash = obj.Hash;
                string prefix = hash[..2];
                string destination = Path.Combine(objectsRoot, prefix, hash);
                string url = $"https://resources.download.minecraft.net/{prefix}/{hash}";

                downloadTasks.Add(new DownloadTask
                {
                    Url = url,
                    DestinationPath = destination,
                    ExpectedSha1 = hash,
                    ExpectedSize = obj.Size
                });
            }

            await DownloadManager.RunAllAsync(downloadTasks, maxConcurrency: 16, ct).ConfigureAwait(false);
        }
    }
}
