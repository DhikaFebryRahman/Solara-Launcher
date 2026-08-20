using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinecraftLauncher.Models
{
    public sealed class VersionDetail
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("mainClass")]
        public string MainClass { get; set; } = string.Empty;

        [JsonPropertyName("minecraftArguments")]
        public string? MinecraftArguments { get; set; }

        [JsonPropertyName("arguments")]
        public ArgumentsSpec? Arguments { get; set; }

        [JsonPropertyName("assetIndex")]
        public AssetIndexInfo AssetIndex { get; set; } = new();

        [JsonPropertyName("assets")]
        public string Assets { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public DownloadsSpec Downloads { get; set; } = new();

        [JsonPropertyName("libraries")]
        public List<LibraryEntry> Libraries { get; set; } = new();

        [JsonPropertyName("javaVersion")]
        public JavaVersionSpec JavaVersion { get; set; } = new();

        [JsonPropertyName("logging")]
        public LoggingSpec? Logging { get; set; }
    }

    public sealed class ArgumentsSpec
    {
        [JsonPropertyName("game")]
        public JsonElement Game { get; set; }

        [JsonPropertyName("jvm")]
        public JsonElement Jvm { get; set; }
    }

    public sealed class AssetIndexInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public sealed class DownloadsSpec
    {
        [JsonPropertyName("client")]
        public ArtifactInfo Client { get; set; } = new();
    }

    public sealed class ArtifactInfo
    {
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        [JsonPropertyName("sha1")]
        public string Sha1 { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }

    public sealed class LibraryEntry
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("downloads")]
        public LibraryDownloads? Downloads { get; set; }

        [JsonPropertyName("natives")]
        public Dictionary<string, string>? Natives { get; set; }

        [JsonPropertyName("rules")]
        public List<Rule>? Rules { get; set; }
    }

    public sealed class LibraryDownloads
    {
        [JsonPropertyName("artifact")]
        public ArtifactInfo? Artifact { get; set; }

        [JsonPropertyName("classifiers")]
        public Dictionary<string, ArtifactInfo>? Classifiers { get; set; }
    }

    public sealed class Rule
    {
        [JsonPropertyName("action")]
        public string Action { get; set; } = "allow";

        [JsonPropertyName("os")]
        public RuleOs? Os { get; set; }

        [JsonPropertyName("features")]
        public Dictionary<string, bool>? Features { get; set; }
    }

    public sealed class RuleOs
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("arch")]
        public string? Arch { get; set; }
    }

    public sealed class JavaVersionSpec
    {
        [JsonPropertyName("component")]
        public string Component { get; set; } = "jre-legacy";

        [JsonPropertyName("majorVersion")]
        public int MajorVersion { get; set; } = 8;
    }

    public sealed class LoggingSpec
    {
        [JsonPropertyName("client")]
        public LoggingClientSpec? Client { get; set; }
    }

    public sealed class LoggingClientSpec
    {
        [JsonPropertyName("argument")]
        public string Argument { get; set; } = string.Empty;

        [JsonPropertyName("file")]
        public ArtifactInfo File { get; set; } = new();

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
    }
}
