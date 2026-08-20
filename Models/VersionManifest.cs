using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinecraftLauncher.Models
{
    public sealed class VersionManifestRoot
    {
        [JsonPropertyName("latest")]
        public LatestVersions Latest { get; set; } = new();

        [JsonPropertyName("versions")]
        public List<VersionManifestEntry> Versions { get; set; } = new();
    }

    public sealed class LatestVersions
    {
        [JsonPropertyName("release")]
        public string Release { get; set; } = string.Empty;

        [JsonPropertyName("snapshot")]
        public string Snapshot { get; set; } = string.Empty;
    }

    public sealed class VersionManifestEntry
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("sha1")]
        public string Sha1 { get; set; } = string.Empty;
    }
}
