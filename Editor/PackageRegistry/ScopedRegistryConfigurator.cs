using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Adds the OpenUPM scoped registry to Packages/manifest.json so that
    /// EDM4U and Firebase packages resolve automatically.
    /// All operations are idempotent — safe to call repeatedly.
    /// </summary>
    public static class ScopedRegistryConfigurator
    {
        public const string RegistryName = "package.openupm.com";
        public const string RegistryUrl = "https://package.openupm.com";

        static readonly string[] RequiredScopes = new[]
        {
            "com.google.external-dependency-manager",
            "com.google.firebase",
            "com.google.play"
        };

        /// <summary>
        /// Ensure the OpenUPM scoped registry with the required scopes exists
        /// in Packages/manifest.json. Returns true if the file was modified.
        /// </summary>
        public static bool EnsureOpenUpmRegistry()
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[BizSim.EditorCore] Packages/manifest.json not found");
                return false;
            }

            string json = File.ReadAllText(manifestPath);
            string modified = EnsureOpenUpmRegistry(json);

            if (modified == json)
                return false;

            File.WriteAllText(manifestPath, modified);
            Debug.Log("[BizSim.EditorCore] Added OpenUPM scoped registry to manifest.json");
            return true;
        }

        /// <summary>
        /// Pure function: given manifest JSON, returns modified JSON with the
        /// OpenUPM scoped registry present. Returns the original string
        /// unchanged if the registry already exists with all required scopes.
        /// Exposed for testability.
        /// </summary>
        public static string EnsureOpenUpmRegistry(string manifestJson)
        {
            if (string.IsNullOrEmpty(manifestJson))
                return manifestJson;

            // Check if the registry already exists with all scopes
            if (HasAllRequiredScopes(manifestJson))
                return manifestJson;

            // Build the scoped registry JSON block
            string scopesJson = "";
            for (int i = 0; i < RequiredScopes.Length; i++)
            {
                scopesJson += $"      \"{RequiredScopes[i]}\"";
                if (i < RequiredScopes.Length - 1) scopesJson += ",\n";
            }

            string registryBlock =
                "{\n" +
                $"      \"name\": \"{RegistryName}\",\n" +
                $"      \"url\": \"{RegistryUrl}\",\n" +
                "      \"scopes\": [\n" +
                $"        {scopesJson.Replace("      ", "        ")}\n" +
                "      ]\n" +
                "    }";

            // Check if scopedRegistries array exists
            if (manifestJson.Contains("\"scopedRegistries\""))
            {
                // Find the opening bracket of the scopedRegistries array
                int srIndex = manifestJson.IndexOf("\"scopedRegistries\"");
                int bracketStart = manifestJson.IndexOf('[', srIndex);
                if (bracketStart < 0) return manifestJson;

                // Check if array is empty
                int bracketEnd = FindMatchingBracket(manifestJson, bracketStart);
                if (bracketEnd < 0) return manifestJson;

                string arrayContent = manifestJson.Substring(bracketStart + 1, bracketEnd - bracketStart - 1).Trim();

                if (string.IsNullOrEmpty(arrayContent))
                {
                    // Empty array — insert the registry
                    string replacement = $"[\n    {registryBlock}\n  ]";
                    manifestJson = manifestJson.Substring(0, bracketStart) + replacement + manifestJson.Substring(bracketEnd + 1);
                }
                else
                {
                    // Non-empty array — append after the last entry
                    manifestJson = manifestJson.Substring(0, bracketEnd) + $",\n    {registryBlock}\n  " + manifestJson.Substring(bracketEnd);
                }
            }
            else
            {
                // No scopedRegistries key — add before the closing brace
                int lastBrace = manifestJson.LastIndexOf('}');
                if (lastBrace < 0) return manifestJson;

                string insertion = $",\n  \"scopedRegistries\": [\n    {registryBlock}\n  ]";
                manifestJson = manifestJson.Substring(0, lastBrace) + insertion + "\n" + manifestJson.Substring(lastBrace);
            }

            return manifestJson;
        }

        /// <summary>
        /// Check whether the manifest already contains the OpenUPM registry
        /// with all required scopes.
        /// </summary>
        static bool HasAllRequiredScopes(string json)
        {
            if (!json.Contains(RegistryUrl)) return false;

            foreach (string scope in RequiredScopes)
            {
                if (!json.Contains(scope)) return false;
            }

            return true;
        }

        static int FindMatchingBracket(string text, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < text.Length; i++)
            {
                if (text[i] == '[') depth++;
                else if (text[i] == ']') depth--;

                if (depth == 0) return i;
            }

            return -1;
        }
    }
}
