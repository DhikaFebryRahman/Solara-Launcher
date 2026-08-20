using System;
using System.IO;
using Microsoft.Win32;

namespace MinecraftLauncher.Config
{
    public sealed class LauncherConfig
    {
        public string PlayerName { get; set; } = "Player";
        public string Version { get; set; } = "1.12.2";
        public int RamMb { get; set; } = 4096;
        public bool KeepLauncherOpen { get; set; } = false;
        public string Theme { get; set; } = "Dark";
        public string InstallDirectory { get; set; } = string.Empty;

        private const string RegistryKeyPath = @"Software\MinecraftLauncher";

        public string GetInstallDirectory()
        {
            if (!string.IsNullOrWhiteSpace(InstallDirectory))
            {
                return InstallDirectory;
            }

            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        }

        public static LauncherConfig Load()
        {
            var config = new LauncherConfig();

            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
                if (key == null)
                {
                    return config;
                }

                if (key.GetValue("PlayerName") is string playerName && !string.IsNullOrEmpty(playerName))
                {
                    config.PlayerName = playerName;
                }

                if (key.GetValue("Version") is string version && !string.IsNullOrEmpty(version))
                {
                    config.Version = version;
                }

                if (key.GetValue("RamMb") is int ramMb && ramMb > 0)
                {
                    config.RamMb = ramMb;
                }

                if (key.GetValue("KeepLauncherOpen") is int keepOpen)
                {
                    config.KeepLauncherOpen = keepOpen != 0;
                }

                if (key.GetValue("Theme") is string theme && !string.IsNullOrEmpty(theme))
                {
                    config.Theme = theme;
                }

                if (key.GetValue("InstallDirectory") is string installDir)
                {
                    config.InstallDirectory = installDir;
                }
            }
            catch (Exception)
            {
            }

            return config;
        }

        public void Save()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath);
                key.SetValue("PlayerName", PlayerName, RegistryValueKind.String);
                key.SetValue("Version", Version, RegistryValueKind.String);
                key.SetValue("RamMb", RamMb, RegistryValueKind.DWord);
                key.SetValue("KeepLauncherOpen", KeepLauncherOpen ? 1 : 0, RegistryValueKind.DWord);
                key.SetValue("Theme", Theme, RegistryValueKind.String);
                key.SetValue("InstallDirectory", InstallDirectory, RegistryValueKind.String);
            }
            catch (Exception)
            {
            }
        }
    }
}
