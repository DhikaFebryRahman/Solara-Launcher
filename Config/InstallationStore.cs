using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Config
{
    public sealed class InstallationStore
    {
        private readonly string _filePath;

        public InstallationStore()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string dir = Path.Combine(appData, "SolaraLauncher");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "installations.json");
        }

        public List<Installation> Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                {
                    return new List<Installation>();
                }

                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Installation>>(json) ?? new List<Installation>();
            }
            catch (Exception)
            {
                return new List<Installation>();
            }
        }

        public void Save(List<Installation> installations)
        {
            try
            {
                string json = JsonSerializer.Serialize(installations, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception)
            {
            }
        }

        public List<Installation> Upsert(string version, string directory)
        {
            var installations = Load();
            installations.RemoveAll(i => string.Equals(i.Version, version, StringComparison.Ordinal)
                                         && string.Equals(i.Directory, directory, StringComparison.OrdinalIgnoreCase));
            installations.Add(new Installation
            {
                Version = version,
                Directory = directory,
                InstalledAt = DateTime.Now
            });
            installations = installations.OrderByDescending(i => i.InstalledAt).ToList();
            Save(installations);
            return installations;
        }

        public List<Installation> Remove(string version, string directory)
        {
            var installations = Load();
            installations.RemoveAll(i => string.Equals(i.Version, version, StringComparison.Ordinal)
                                         && string.Equals(i.Directory, directory, StringComparison.OrdinalIgnoreCase));
            Save(installations);
            return installations;
        }
    }
}
