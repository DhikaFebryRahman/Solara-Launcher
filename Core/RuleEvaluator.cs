using System.Collections.Generic;
using MinecraftLauncher.Models;

namespace MinecraftLauncher.Core
{
    public static class RuleEvaluator
    {
        public static bool IsAllowed(List<Rule>? rules)
        {
            if (rules == null || rules.Count == 0)
            {
                return true;
            }

            bool allowed = false;

            foreach (var rule in rules)
            {
                if (!Matches(rule))
                {
                    continue;
                }

                allowed = rule.Action == "allow";
            }

            return allowed;
        }

        private static bool Matches(Rule rule)
        {
            if (rule.Features is { Count: > 0 })
            {
                return false;
            }

            if (rule.Os == null)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(rule.Os.Name) && rule.Os.Name != "windows")
            {
                return false;
            }

            if (!string.IsNullOrEmpty(rule.Os.Arch))
            {
                bool is64BitArch = rule.Os.Arch.Contains("64");
                if (!is64BitArch)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
