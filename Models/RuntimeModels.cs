using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftLauncher.Models
{
    public sealed class AssetIndexFile
    {
        [JsonPropertyName("objects")]
        public Dictionary<string, AssetObjectInfo> Objects { get; set; } = new();
    }

    public sealed class AssetObjectInfo
    {
        [JsonPropertyName("hash")]
        public string Hash { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public sealed class JavaRuntimeAllJson
    {
        [JsonPropertyName("windows-x64")]
        public Dictionary<string, List<JavaRuntimeComponentEntry>>? WindowsX64 { get; set; }
    }

    public sealed class JavaRuntimeComponentEntry
    {
        [JsonPropertyName("manifest")]
        public ArtifactInfo Manifest { get; set; } = new();

        [JsonPropertyName("version")]
        public JavaRuntimeVersionInfo? Version { get; set; }
    }

    public sealed class JavaRuntimeVersionInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public sealed class JavaRuntimeFileManifest
    {
        [JsonPropertyName("files")]
        public Dictionary<string, JavaRuntimeFileEntry> Files { get; set; } = new();
    }

    public sealed class JavaRuntimeFileEntry
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("executable")]
        public bool Executable { get; set; }

        [JsonPropertyName("downloads")]
        public JavaRuntimeFileDownloads? Downloads { get; set; }
    }

    public sealed class JavaRuntimeFileDownloads
    {
        [JsonPropertyName("raw")]
        public ArtifactInfo Raw { get; set; } = new();
    }
}
