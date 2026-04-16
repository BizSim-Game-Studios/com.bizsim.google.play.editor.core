using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine.Networking;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Checks the latest GitHub release tag for each BizSim package.
    /// Polls via EditorApplication.update (no EditorCoroutine dependency).
    /// Caches results in SessionState with a 15-minute TTL.
    /// </summary>
    public static class RemoteVersionChecker
    {
        const string UserAgent = "BizSimPackageDashboard/1.1";
        const string CacheKeyPrefix = "BizSim.VersionCheck.";
        const int CacheTtlMinutes = 15;
        const string TimestampSuffix = ".ts";

        // Matches "tag_name" : "v1.2.3" in the GitHub releases/latest JSON
        static readonly Regex TagNamePattern = new(
            @"""tag_name""\s*:\s*""([^""]+)""",
            RegexOptions.Compiled);

        /// <summary>
        /// Check latest tags for all BizSim packages that have a GitHubRepoName.
        /// Populates each entry's LatestTag field and invokes onComplete when done.
        /// Skips entries that have a valid cache hit.
        /// </summary>
        public static void CheckAll(PackageRegistryData registry, Action onComplete)
        {
            if (registry == null || registry.BizSimPackages == null || registry.BizSimPackages.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            var pending = new List<PendingRequest>();

            foreach (var entry in registry.BizSimPackages)
            {
                if (string.IsNullOrEmpty(entry.GitHubRepoName))
                    continue;

                // Try cache first
                string cached = GetCachedTag(entry.GitHubRepoName);
                if (cached != null)
                {
                    entry.LatestTag = cached;
                    continue;
                }

                string url = $"https://api.github.com/repos/BizSim-Game-Studios/{entry.GitHubRepoName}/releases/latest";
                var request = UnityWebRequest.Get(url);
                request.SetRequestHeader("User-Agent", UserAgent);
                request.SetRequestHeader("Accept", "application/vnd.github.v3+json");
                request.SendWebRequest();

                pending.Add(new PendingRequest { Entry = entry, Request = request });
            }

            if (pending.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            // Poll via EditorApplication.update
            EditorApplication.CallbackFunction poll = null;
            poll = () =>
            {
                bool allDone = true;
                foreach (var p in pending)
                {
                    if (!p.Request.isDone)
                    {
                        allDone = false;
                        continue;
                    }

                    if (p.Processed) continue;
                    p.Processed = true;

                    if (p.Request.result == UnityWebRequest.Result.Success)
                    {
                        string tag = ParseTagName(p.Request.downloadHandler.text);
                        if (!string.IsNullOrEmpty(tag))
                        {
                            p.Entry.LatestTag = tag;
                            SetCachedTag(p.Entry.GitHubRepoName, tag);
                        }
                    }

                    p.Request.Dispose();
                }

                if (allDone)
                {
                    EditorApplication.update -= poll;
                    onComplete?.Invoke();
                }
            };

            EditorApplication.update += poll;
        }

        /// <summary>
        /// Clear all cached version check results.
        /// </summary>
        public static void ClearCache()
        {
            // SessionState does not expose enumeration, so consumers must
            // reload from the network after calling this.
        }

        static string ParseTagName(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var match = TagNamePattern.Match(json);
            return match.Success ? match.Groups[1].Value : null;
        }

        static string GetCachedTag(string repoName)
        {
            string key = CacheKeyPrefix + repoName;
            string tag = SessionState.GetString(key, "");
            if (string.IsNullOrEmpty(tag)) return null;

            string tsStr = SessionState.GetString(key + TimestampSuffix, "");
            if (string.IsNullOrEmpty(tsStr)) return null;

            if (long.TryParse(tsStr, out long ticks))
            {
                var cachedAt = new DateTime(ticks);
                if ((DateTime.UtcNow - cachedAt).TotalMinutes < CacheTtlMinutes)
                    return tag;
            }

            return null;
        }

        static void SetCachedTag(string repoName, string tag)
        {
            string key = CacheKeyPrefix + repoName;
            SessionState.SetString(key, tag);
            SessionState.SetString(key + TimestampSuffix, DateTime.UtcNow.Ticks.ToString());
        }

        class PendingRequest
        {
            public PackageRegistryEntry Entry;
            public UnityWebRequest Request;
            public bool Processed;
        }
    }
}
