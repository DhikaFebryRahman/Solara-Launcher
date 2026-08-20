using System;
using System.IO;
using System.Security.Cryptography;

namespace MinecraftLauncher.Core
{
    public static class HashUtil
    {
        public static string ComputeSha1(string filePath)
        {
            using var sha1 = SHA1.Create();
            using var stream = File.OpenRead(filePath);
            byte[] hash = sha1.ComputeHash(stream);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool IsValid(string filePath, string expectedSha1, long expectedSize)
        {
            if (!File.Exists(filePath))
            {
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (expectedSize > 0 && fileInfo.Length != expectedSize)
            {
                return false;
            }

            if (string.IsNullOrEmpty(expectedSha1))
            {
                return true;
            }

            string actual = ComputeSha1(filePath);
            return string.Equals(actual, expectedSha1, StringComparison.OrdinalIgnoreCase);
        }
    }
}
