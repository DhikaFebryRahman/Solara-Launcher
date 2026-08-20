using System.Collections.Generic;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Core
{
    public sealed class InstallResult
    {
        public required string GameDirectory { get; init; }
        public required string AssetsRoot { get; init; }
        public required string NativesDirectory { get; init; }
        public required string JavaExecutablePath { get; init; }
        public required VersionDetail VersionDetail { get; init; }
        public required List<string> ClasspathEntries { get; init; }
        public string? LoggingArgumentTemplate { get; init; }
        public string? LoggingConfigFilePath { get; init; }
    }
}
