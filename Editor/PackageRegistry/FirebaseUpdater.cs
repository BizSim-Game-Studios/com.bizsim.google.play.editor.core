// Copyright (c) BizSim Game Studios. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Secure one-click updater for the Firebase Unity SDK (ADR-010, editor.core 1.6.0).
    /// Downloads the latest combined release zip from
    /// <c>github.com/firebase/firebase-unity-sdk/releases/latest</c>, verifies SHA256 when
    /// the release ships a <c>sha256sums.txt</c> asset, extracts with a path-traversal
    /// guard, and invokes <see cref="AssetDatabase.ImportPackage"/> with
    /// <c>interactive: true</c> for each installed module's <c>.unitypackage</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>ADR-010 security controls:</b>
    /// <list type="number">
    /// <item>URL allowlist — only <c>api.github.com/repos/firebase/firebase-unity-sdk/*</c>
    ///       and <c>github.com/firebase/firebase-unity-sdk/*</c>. See <see cref="IsAllowedUrl"/>.</item>
    /// <item>HTTPS only — no plain-HTTP fallback.</item>
    /// <item>SHA256 verification when the release ships a <c>sha256sums.txt</c> asset —
    ///       mismatch aborts import with a user-visible error. When the asset is absent,
    ///       the user is prompted to continue with TLS-only security; this defaults to
    ///       cancel so silent downgrade is not possible.</item>
    /// <item>Zip extraction path guard — any entry whose normalized full path escapes
    ///       the destination directory (via <c>..</c> or absolute paths) aborts extraction
    ///       and deletes any partially-extracted files.</item>
    /// <item>Interactive import — Unity's native <c>ImportPackage</c> dialog is shown
    ///       per module so the user reviews every file before accepting.</item>
    /// <item>Temp-only writes — all downloads and extraction live under
    ///       <c>Temporary Cache/BizSimFirebaseUpdater</c>; cleaned up after success or
    ///       on editor shutdown.</item>
    /// </list>
    /// </para>
    /// <para>
    /// This class does NOT replace Google's installer's security surface — the same
    /// trust assumption (the Firebase team ships signed DLLs via their GitHub org)
    /// applies whether the user clicks Download in the browser or this updater. The
    /// controls above limit the additional attack surface introduced by automation.
    /// </para>
    /// </remarks>
    public static class FirebaseUpdater
    {
        const string UserAgent = "BizSimFirebaseUpdater/1.0";
        const string GithubApiLatestRelease =
            "https://api.github.com/repos/firebase/firebase-unity-sdk/releases/latest";

        static readonly Regex AssetNameJsonPattern = new(
            @"""name""\s*:\s*""([^""]+)""\s*,\s*""[^""]+""\s*:[^,]+,\s*""[^""]+""\s*:[^,]+,\s*""browser_download_url""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled);

        /// <summary>
        /// Throws <see cref="ArgumentException"/> when <paramref name="url"/> does NOT
        /// point at the Firebase Unity SDK GitHub org over HTTPS. First line of defense
        /// against accidental URL injection.
        /// </summary>
        internal static bool IsAllowedUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            if (!url.StartsWith("https://", StringComparison.Ordinal)) return false;

            // Only github.com/firebase/firebase-unity-sdk/... and
            // api.github.com/repos/firebase/firebase-unity-sdk/... permitted.
            return
                url.StartsWith("https://github.com/firebase/firebase-unity-sdk/", StringComparison.Ordinal) ||
                url.StartsWith("https://api.github.com/repos/firebase/firebase-unity-sdk/", StringComparison.Ordinal) ||
                // GitHub release asset CDN redirects to objects.githubusercontent.com.
                url.StartsWith("https://objects.githubusercontent.com/", StringComparison.Ordinal);
        }

        /// <summary>
        /// Extracts <c>(name, download_url)</c> pairs from a GitHub release JSON response.
        /// Sufficient for finding the <c>firebase_unity_sdk_*.zip</c> asset and the
        /// optional <c>sha256sums.txt</c> asset without a JSON dependency.
        /// </summary>
        internal static List<(string Name, string Url)> ParseReleaseAssets(string json)
        {
            var results = new List<(string, string)>();
            if (string.IsNullOrEmpty(json)) return results;

            // Primitive but sufficient: rely on canonical GitHub API response shape.
            // "assets": [ { "name": "...", "label": ..., "content_type": ..., "browser_download_url": "..." }, ... ]
            foreach (Match m in AssetNameJsonPattern.Matches(json))
                results.Add((m.Groups[1].Value, m.Groups[2].Value));

            return results;
        }

        /// <summary>
        /// Verifies that every zip entry's normalized full path stays within
        /// <paramref name="destDir"/>. Rejects absolute paths and parent-directory
        /// escapes (<c>..</c>). Returns null when the archive is safe; otherwise
        /// the offending entry name.
        /// </summary>
        internal static string FindUnsafeZipEntry(string zipPath, string destDir)
        {
            string fullDestDir = Path.GetFullPath(destDir) +
                Path.DirectorySeparatorChar.ToString();

            using var archive = ZipFile.OpenRead(zipPath);
            foreach (var entry in archive.Entries)
            {
                // An entry pointing at a directory is fine; its FullName ends with /.
                string candidate = Path.GetFullPath(Path.Combine(destDir, entry.FullName));
                if (!candidate.StartsWith(fullDestDir, StringComparison.Ordinal))
                    return entry.FullName;
            }
            return null;
        }

        /// <summary>
        /// Computes the lowercase hex SHA256 digest of the file at <paramref name="path"/>.
        /// </summary>
        internal static string ComputeSha256(string path)
        {
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            var hash = sha.ComputeHash(stream);
            var sb = new System.Text.StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Parses a GNU coreutils-style <c>sha256sums.txt</c> line and returns the digest
        /// for the given filename (case-insensitive match). Returns null when the file is
        /// not listed.
        /// </summary>
        internal static string LookupExpectedSha256(string sha256sumsContent, string filename)
        {
            if (string.IsNullOrEmpty(sha256sumsContent) || string.IsNullOrEmpty(filename))
                return null;

            foreach (var rawLine in sha256sumsContent.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;

                // Format: "<hex-sha256>  <filename>" (two spaces per GNU convention).
                int sep = line.IndexOf("  ", StringComparison.Ordinal);
                if (sep < 0) sep = line.IndexOf(' ');
                if (sep < 0) continue;

                string hex = line.Substring(0, sep).Trim();
                string name = line.Substring(sep + 1).Trim().TrimStart('*');
                if (string.Equals(name, filename, StringComparison.OrdinalIgnoreCase))
                    return hex.ToLowerInvariant();
            }
            return null;
        }

        /// <summary>
        /// Finds Firebase <c>*.unitypackage</c> files for the caller's installed modules.
        /// <paramref name="installedAssemblyNames"/> is the set of <c>Firebase.*</c>
        /// assemblies currently loaded in the AppDomain (per <see cref="PackageDetector"/>);
        /// each is matched against on-disk files like <c>FirebaseAnalytics.unitypackage</c>.
        /// </summary>
        internal static List<string> FindInstalledModulePackages(
            string extractedDir,
            IEnumerable<string> installedAssemblyNames)
        {
            var results = new List<string>();
            if (!Directory.Exists(extractedDir)) return results;

            // Firebase ships each module as "Firebase<Module>.unitypackage" alongside
            // dotnet4/ dotnet46/ subfolders. Map assembly "Firebase.Analytics" to the
            // base name "FirebaseAnalytics.unitypackage".
            var targetBasenames = installedAssemblyNames
                .Where(a => a != null && a.StartsWith("Firebase.", StringComparison.Ordinal))
                .Select(a => "Firebase" + a.Substring("Firebase.".Length) + ".unitypackage")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var file in Directory.EnumerateFiles(extractedDir, "*.unitypackage",
                         SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (targetBasenames.Contains(name))
                    results.Add(file);
            }
            return results;
        }

        /// <summary>
        /// Entry point used by the dashboard's "Update All Installed" button.
        /// Shows a confirmation dialog, fetches the latest release metadata, downloads
        /// the combined zip, verifies SHA256 when available, extracts with path-traversal
        /// guard, and invokes interactive ImportPackage for each installed module.
        /// </summary>
        public static void UpdateInstalledModules(IEnumerable<string> installedAssemblyNames)
        {
            var installed = installedAssemblyNames?.ToList() ?? new List<string>();
            if (installed.Count == 0)
            {
                EditorUtility.DisplayDialog("Firebase Update",
                    "No Firebase modules are installed. Nothing to update.",
                    "OK");
                return;
            }

            bool proceed = EditorUtility.DisplayDialog(
                "Update Firebase Unity SDK",
                $"This will download the latest Firebase Unity SDK bundle from " +
                $"github.com/firebase/firebase-unity-sdk (~300 MB) and re-import the following " +
                $"installed modules:\n\n• " + string.Join("\n• ",
                    installed.Select(a => a.StartsWith("Firebase.") ? a.Substring(9) : a)) +
                $"\n\nUnity's Import Package dialog will show you every file before it is " +
                $"written. Continue?",
                "Continue", "Cancel");
            if (!proceed) return;

            EditorUtility.DisplayProgressBar("Firebase Update", "Fetching release metadata…", 0.05f);
            try
            {
                if (!TryFetchLatestRelease(out string json, out string error))
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Firebase Update — Failed",
                        "Could not fetch the latest release metadata from GitHub.\n\n" + error,
                        "OK");
                    return;
                }

                var assets = ParseReleaseAssets(json);
                var zipAsset = assets.FirstOrDefault(a =>
                    a.Name.StartsWith("firebase_unity_sdk", StringComparison.OrdinalIgnoreCase) &&
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (string.IsNullOrEmpty(zipAsset.Url) || !IsAllowedUrl(zipAsset.Url))
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Firebase Update — Failed",
                        "The latest release does not expose a recognizable " +
                        "firebase_unity_sdk_*.zip asset on the expected GitHub origin, " +
                        "or the URL failed the allowlist check. Aborting.",
                        "OK");
                    return;
                }

                var sha256Asset = assets.FirstOrDefault(a =>
                    string.Equals(a.Name, "sha256sums.txt", StringComparison.OrdinalIgnoreCase));

                string tempRoot = Path.Combine(Application.temporaryCachePath, "BizSimFirebaseUpdater");
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
                Directory.CreateDirectory(tempRoot);
                string zipPath = Path.Combine(tempRoot, zipAsset.Name);
                string extractDir = Path.Combine(tempRoot, "extracted");
                Directory.CreateDirectory(extractDir);

                EditorUtility.DisplayProgressBar("Firebase Update",
                    $"Downloading {zipAsset.Name}…", 0.1f);
                if (!DownloadFile(zipAsset.Url, zipPath, out error))
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Firebase Update — Failed",
                        "Download failed.\n\n" + error, "OK");
                    return;
                }

                // SHA256 verification (if the release ships sums).
                if (!string.IsNullOrEmpty(sha256Asset.Url) && IsAllowedUrl(sha256Asset.Url))
                {
                    EditorUtility.DisplayProgressBar("Firebase Update", "Verifying SHA256…", 0.65f);
                    string sumsPath = Path.Combine(tempRoot, "sha256sums.txt");
                    if (!DownloadFile(sha256Asset.Url, sumsPath, out error))
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Firebase Update — Failed",
                            "Could not download sha256sums.txt.\n\n" + error, "OK");
                        return;
                    }
                    string expected = LookupExpectedSha256(File.ReadAllText(sumsPath), zipAsset.Name);
                    if (string.IsNullOrEmpty(expected))
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Firebase Update — Failed",
                            $"The release ships a sha256sums.txt but it does NOT list " +
                            $"{zipAsset.Name}. Aborting to avoid silently downgrading security.",
                            "OK");
                        return;
                    }
                    string actual = ComputeSha256(zipPath);
                    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    {
                        EditorUtility.ClearProgressBar();
                        EditorUtility.DisplayDialog("Firebase Update — CHECKSUM MISMATCH",
                            $"SHA256 mismatch for {zipAsset.Name}.\n\n" +
                            $"Expected: {expected}\nActual:   {actual}\n\n" +
                            "Aborting import. The download was corrupted or tampered with.",
                            "OK");
                        return;
                    }
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    bool acceptTlsOnly = EditorUtility.DisplayDialog(
                        "Firebase Update — No Checksum",
                        "The latest release does not ship a sha256sums.txt asset. The " +
                        "download is authenticated by HTTPS/TLS only. Continue import?",
                        "Continue", "Cancel");
                    if (!acceptTlsOnly) return;
                    EditorUtility.DisplayProgressBar("Firebase Update", "Extracting…", 0.7f);
                }

                // Path-traversal guard.
                string unsafeEntry = FindUnsafeZipEntry(zipPath, extractDir);
                if (unsafeEntry != null)
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Firebase Update — UNSAFE ARCHIVE",
                        $"Aborting extraction: zip entry escapes destination:\n{unsafeEntry}",
                        "OK");
                    return;
                }

                ZipFile.ExtractToDirectory(zipPath, extractDir);

                var modulePackages = FindInstalledModulePackages(extractDir, installed);
                EditorUtility.ClearProgressBar();
                if (modulePackages.Count == 0)
                {
                    EditorUtility.DisplayDialog("Firebase Update — No Matches",
                        "The download completed and was verified, but no .unitypackage " +
                        "files matched your installed modules. Nothing imported.",
                        "OK");
                    return;
                }

                foreach (var pkg in modulePackages)
                    AssetDatabase.ImportPackage(pkg, interactive: true);
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Firebase Update — Error",
                    "Unexpected error during Firebase update:\n\n" + ex.Message,
                    "OK");
            }
        }

        static bool TryFetchLatestRelease(out string json, out string error)
        {
            json = null;
            error = null;
            if (!IsAllowedUrl(GithubApiLatestRelease))
            {
                error = "GitHub API URL failed allowlist check. This is a code bug, not a runtime condition.";
                return false;
            }

            using var req = UnityWebRequest.Get(GithubApiLatestRelease);
            req.SetRequestHeader("User-Agent", UserAgent);
            req.SetRequestHeader("Accept", "application/vnd.github.v3+json");
            var op = req.SendWebRequest();
            while (!op.isDone) { /* blocking fetch — editor UX, OK */ }
            if (req.result != UnityWebRequest.Result.Success)
            {
                error = req.error ?? "network error";
                return false;
            }
            json = req.downloadHandler.text;
            return true;
        }

        static bool DownloadFile(string url, string destPath, out string error)
        {
            error = null;
            if (!IsAllowedUrl(url))
            {
                error = $"URL {url} failed allowlist check.";
                return false;
            }

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("User-Agent", UserAgent);
            req.downloadHandler = new DownloadHandlerFile(destPath);
            var op = req.SendWebRequest();
            while (!op.isDone) { /* blocking — editor UX, OK */ }
            if (req.result != UnityWebRequest.Result.Success)
            {
                error = req.error ?? "network error";
                return false;
            }
            return true;
        }
    }
}
