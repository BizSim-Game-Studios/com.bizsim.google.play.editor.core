using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Reads Packages/manifest.json to determine installed package versions.
    /// Handles both registry versions ("1.2.3") and git URL versions
    /// ("https://...#v1.2.3" or "https://...#1.2.3").
    /// </summary>
    public static class ManifestReader
    {
        // Matches: "com.some.package": "1.2.3"  or  "com.some.package": "https://...#v1.2.3"
        static readonly Regex DependencyPattern = new(
            @"""([^""]+)""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled);

        // Extracts version from a git URL fragment: ...#v1.2.3 or ...#1.2.3
        static readonly Regex GitTagPattern = new(
            @"#v?(\d+\.\d+\.\d+(?:[-.].+)?)$",
            RegexOptions.Compiled);

        // Matches a plain semver version string
        static readonly Regex SemverPattern = new(
            @"^\d+\.\d+\.\d+",
            RegexOptions.Compiled);

        /// <summary>
        /// Read the project's Packages/manifest.json and return a dictionary
        /// of packageId to installed version string.
        /// Returns an empty dictionary if the file cannot be read.
        /// </summary>
        public static Dictionary<string, string> ReadInstalledVersions()
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[BizSim.EditorCore] Packages/manifest.json not found");
                return new Dictionary<string, string>();
            }

            string json = File.ReadAllText(manifestPath);
            return ParseManifest(json);
        }

        /// <summary>
        /// Parse manifest JSON content and extract package versions.
        /// Exposed for testability with mock JSON strings.
        /// </summary>
        public static Dictionary<string, string> ParseManifest(string json)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(json)) return result;

            // Extract the "dependencies" block
            int depIndex = json.IndexOf("\"dependencies\"");
            if (depIndex < 0) return result;

            int braceStart = json.IndexOf('{', depIndex);
            if (braceStart < 0) return result;

            int braceEnd = FindMatchingBrace(json, braceStart);
            if (braceEnd < 0) return result;

            string depBlock = json.Substring(braceStart, braceEnd - braceStart + 1);

            foreach (Match match in DependencyPattern.Matches(depBlock))
            {
                string packageId = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                string version = ExtractVersion(value);
                if (!string.IsNullOrEmpty(version))
                {
                    result[packageId] = version;
                }
            }

            return result;
        }

        /// <summary>
        /// Extract a semver version from either a plain version string or a git URL.
        /// </summary>
        internal static string ExtractVersion(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            // Plain semver: "1.2.3"
            if (SemverPattern.IsMatch(value))
                return value;

            // Git URL with tag: "https://github.com/.../repo.git#v1.2.3"
            var gitMatch = GitTagPattern.Match(value);
            if (gitMatch.Success)
                return gitMatch.Groups[1].Value;

            // file: path or other format — no extractable version
            return null;
        }

        static int FindMatchingBrace(string text, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}') depth--;

                if (depth == 0) return i;
            }

            return -1;
        }
    }
}
