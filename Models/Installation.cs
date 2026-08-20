using System;

namespace MinecraftLauncher.Models
{
    public sealed class Installation
    {
        public string Version { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public DateTime InstalledAt { get; set; } = DateTime.Now;
    }
}
