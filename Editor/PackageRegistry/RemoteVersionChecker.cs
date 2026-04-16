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

        const string FirebaseRepo = "firebase/firebase-unity-sdk";
        const string FirebaseCacheKey = CacheKeyPrefix + "firebase-unity-sdk";
        const string GooglePlayPluginsRepo = "google/play-unity-plugins";
        const string GooglePlayCacheKey = CacheKeyPrefix + "play-unity-plugins";

        static int _pendingCheckCount;

        /// <summary>
        /// True while one or more version check requests are in flight.
        /// </summary>
        public static bool IsChecking => _pendingCheckCount > 0;

        /// <summary>Latest Firebase Unity SDK tag (e.g. "v12.7.0"), or null.</summary>
        public static string LatestFirebaseTag { get; private set; }

        /// <summary>Latest Google Play Unity Plugins tag, or null.</summary>
        public static string LatestGooglePlayPluginsTag { get; private set; }

        /// <summary>
        /// Check latest tags for all BizSim packages, Firebase SDK, and Google Play Plugins.
        /// Populates each entry's LatestTag field and invokes onComplete when done.
        /// Skips entries that have a valid cache hit.
        /// </summary>
        public static void CheckAll(PackageRegistryData registry, Action onComplete)
        {
            if (registry == null)
            {
                onComplete?.Invoke();
                return;
            }

            var pending = new List<PendingRequest>();

            // BizSim packages
            if (registry.BizSimPackages != null)
            {
                foreach (var entry in registry.BizSimPackages)
                {
                    if (string.IsNullOrEmpty(entry.GitHubRepoName))
                        continue;

                    string cached = GetCachedTag(entry.GitHubRepoName);
                    if (cached != null)
                    {
                        entry.LatestTag = cached;
                        continue;
                    }

                    string url = $"https://api.github.com/repos/BizSim-Game-Studios/{entry.GitHubRepoName}/releases/latest";
                    pending.Add(CreatePendingRequest(entry, url));
                }
            }

            // Firebase Unity SDK
            string fbCached = GetCachedTag("firebase-unity-sdk");
            if (fbCached != null)
                LatestFirebaseTag = fbCached;
            else
                pending.Add(CreatePendingRequest(null,
                    $"https://api.github.com/repos/{FirebaseRepo}/releases/latest",
                    "firebase-unity-sdk"));

            // Google Play Unity Plugins
            string gpCached = GetCachedTag("play-unity-plugins");
            if (gpCached != null)
                LatestGooglePlayPluginsTag = gpCached;
            else
                pending.Add(CreatePendingRequest(null,
                    $"https://api.github.com/repos/{GooglePlayPluginsRepo}/releases/latest",
                    "play-unity-plugins"));

            if (pending.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            _pendingCheckCount++;

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
                            if (p.Entry != null)
                            {
                                p.Entry.LatestTag = tag;
                                SetCachedTag(p.Entry.GitHubRepoName, tag);
                            }
                            else if (p.CacheKey == "firebase-unity-sdk")
                            {
                                LatestFirebaseTag = tag;
                                SetCachedTag("firebase-unity-sdk", tag);
                            }
                            else if (p.CacheKey == "play-unity-plugins")
                            {
                                LatestGooglePlayPluginsTag = tag;
                                SetCachedTag("play-unity-plugins", tag);
                            }
                        }
                    }

                    p.Request.Dispose();
                }

                if (allDone)
                {
                    _pendingCheckCount--;
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

        static PendingRequest CreatePendingRequest(PackageRegistryEntry entry, string url, string cacheKey = null)
        {
            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("User-Agent", UserAgent);
            request.SetRequestHeader("Accept", "application/vnd.github.v3+json");
            request.SendWebRequest();
            return new PendingRequest { Entry = entry, Request = request, CacheKey = cacheKey };
        }

        class PendingRequest
        {
            public PackageRegistryEntry Entry;
            public UnityWebRequest Request;
            public bool Processed;
            public string CacheKey;
        }
    }
}
