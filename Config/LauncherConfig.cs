using System;
using Microsoft.Win32;

namespace MinecraftLauncher.Config
{
    public sealed class LauncherConfig
    {
        public string PlayerName { get; set; } = "Player";
        public string Version { get; set; } = "1.12.2";
        public int RamMb { get; set; } = 4096;

        private const string RegistryKeyPath = @"Software\MinecraftLauncher";

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
            }
            catch (Exception)
            {
            }
        }
    }
}
