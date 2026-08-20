using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Core
{
    public static class GameLauncher
    {
        public static Process Launch(InstallResult install, string playerName, int ramMb)
        {
            string uuid = GenerateOfflineUuid(playerName);
            string classpath = string.Join(";", install.ClasspathEntries.Distinct());
            var vars = BuildSubstitutionMap(install, playerName, uuid, classpath);

            var psi = new ProcessStartInfo
            {
                FileName = install.JavaExecutablePath,
                WorkingDirectory = install.GameDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            psi.ArgumentList.Add($"-Xmx{ramMb}M");
            psi.ArgumentList.Add($"-Xms{Math.Min(ramMb, 512)}M");

            var jvmArgs = BuildJvmArgs(install, vars);
            foreach (string arg in jvmArgs)
            {
                psi.ArgumentList.Add(arg);
            }

            if (!string.IsNullOrEmpty(install.LoggingArgumentTemplate) && !string.IsNullOrEmpty(install.LoggingConfigFilePath))
            {
                psi.ArgumentList.Add(Substitute(install.LoggingArgumentTemplate, vars).Replace("${path}", install.LoggingConfigFilePath));
            }

            psi.ArgumentList.Add(install.VersionDetail.MainClass);

            var gameArgs = BuildGameArgs(install, vars);
            foreach (string arg in gameArgs)
            {
                psi.ArgumentList.Add(arg);
            }

            var process = new Process { StartInfo = psi };
            process.Start();
            return process;
        }

        private static Dictionary<string, string> BuildSubstitutionMap(InstallResult install, string playerName, string uuid, string classpath)
        {
            return new Dictionary<string, string>
            {
                ["auth_player_name"] = playerName,
                ["version_name"] = install.VersionDetail.Id,
                ["game_directory"] = install.GameDirectory,
                ["assets_root"] = install.AssetsRoot,
                ["game_assets"] = install.AssetsRoot,
                ["assets_index_name"] = install.VersionDetail.Assets,
                ["auth_uuid"] = uuid,
                ["auth_access_token"] = "0",
                ["auth_session"] = "token:0:" + uuid,
                ["user_type"] = "legacy",
                ["version_type"] = "release",
                ["natives_directory"] = install.NativesDirectory,
                ["launcher_name"] = "MinecraftLauncher",
                ["launcher_version"] = "1.0",
                ["classpath"] = classpath,
                ["user_properties"] = "{}",
                ["clientid"] = "0",
                ["auth_xuid"] = "0"
            };
        }

        private static List<string> BuildJvmArgs(InstallResult install, Dictionary<string, string> vars)
        {
            var result = new List<string>();

            var args = install.VersionDetail.Arguments;
            if (args != null && args.Jvm.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in args.Jvm.EnumerateArray())
                {
                    AppendTemplatedElement(element, vars, result);
                }
            }
            else
            {
                result.Add($"-Djava.library.path={vars["natives_directory"]}");
                result.Add("-Dminecraft.launcher.brand=MinecraftLauncher");
                result.Add("-Dminecraft.launcher.version=1.0");
                result.Add("-cp");
                result.Add(vars["classpath"]);
            }

            return result;
        }

        private static List<string> BuildGameArgs(InstallResult install, Dictionary<string, string> vars)
        {
            var result = new List<string>();
            var args = install.VersionDetail.Arguments;

            if (args != null && args.Game.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in args.Game.EnumerateArray())
                {
                    AppendTemplatedElement(element, vars, result);
                }
            }
            else if (!string.IsNullOrEmpty(install.VersionDetail.MinecraftArguments))
            {
                foreach (string token in install.VersionDetail.MinecraftArguments.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    result.Add(Substitute(token, vars));
                }
            }

            return result;
        }

        private static void AppendTemplatedElement(JsonElement element, Dictionary<string, string> vars, List<string> output)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                output.Add(Substitute(element.GetString() ?? string.Empty, vars));
                return;
            }

            if (element.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            List<Models.Rule>? rules = null;
            if (element.TryGetProperty("rules", out var rulesElement))
            {
                rules = rulesElement.Deserialize<List<Models.Rule>>();
            }

            if (!RuleEvaluator.IsAllowed(rules))
            {
                return;
            }

            if (!element.TryGetProperty("value", out var valueElement))
            {
                return;
            }

            if (valueElement.ValueKind == JsonValueKind.String)
            {
                output.Add(Substitute(valueElement.GetString() ?? string.Empty, vars));
            }
            else if (valueElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    output.Add(Substitute(item.GetString() ?? string.Empty, vars));
                }
            }
        }

        private static string Substitute(string template, Dictionary<string, string> vars)
        {
            if (!template.Contains("${"))
            {
                return template;
            }

            var sb = new StringBuilder(template);
            foreach (var (key, value) in vars)
            {
                sb.Replace("${" + key + "}", value);
            }
            return sb.ToString();
        }

        private static string GenerateOfflineUuid(string playerName)
        {
            byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("OfflinePlayer:" + playerName));
            hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
            hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

            string hex = Convert.ToHexString(hash).ToLowerInvariant();
            return $"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..32]}";
        }
    }
}
