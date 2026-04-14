using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Manages scripting define symbols for all BizSim Google Play packages.
    /// Single source of truth — all packages reference this shared copy.
    /// </summary>
    public static class BizSimDefineManager
    {
        public const string FIREBASE_DEFINE = "BIZSIM_FIREBASE";

        /// <summary>
        /// Check if Firebase Analytics is installed (delegates to PackageDetector).
        /// </summary>
        public static bool IsFirebaseAnalyticsInstalled()
        {
            return PackageDetector.IsFirebaseAnalyticsInstalled();
        }

        /// <summary>
        /// Get installed Firebase Analytics version.
        /// </summary>
        public static string GetFirebaseAnalyticsVersion()
        {
            return PackageDetector.GetFirebaseAnalyticsVersion() ?? "Not Found";
        }

        /// <summary>
        /// Check if a define symbol is present for the given build target.
        /// </summary>
        public static bool IsDefinePresent(string define, BuildTargetGroup targetGroup)
        {
            if (targetGroup == BuildTargetGroup.Unknown) return false;

            NamedBuildTarget namedTarget = GetNamedBuildTarget(targetGroup);
            if (namedTarget == NamedBuildTarget.Unknown) return false;
            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            return defines.Split(';').Contains(define);
        }

        /// <summary>
        /// Check if BIZSIM_FIREBASE define is present for the given build target.
        /// </summary>
        public static bool IsFirebaseDefinePresent(BuildTargetGroup targetGroup)
        {
            return IsDefinePresent(FIREBASE_DEFINE, targetGroup);
        }

        /// <summary>
        /// Check if BIZSIM_FIREBASE is present on ANY relevant platform.
        /// </summary>
        public static bool IsFirebaseDefinePresentAnywhere()
        {
            return GetRelevantPlatforms().Any(IsFirebaseDefinePresent);
        }

        /// <summary>
        /// Add a define symbol to the specified platforms.
        /// </summary>
        public static void AddDefine(string define, params BuildTargetGroup[] targetGroups)
        {
            foreach (var targetGroup in targetGroups)
            {
                if (targetGroup == BuildTargetGroup.Unknown) continue;

                NamedBuildTarget namedTarget = GetNamedBuildTarget(targetGroup);
                if (namedTarget == NamedBuildTarget.Unknown) continue;
                string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

                var defineList = defines.Split(';').ToList();

                if (!defineList.Contains(define))
                {
                    defineList.Add(define);
                    string newDefines = string.Join(";", defineList.Where(d => !string.IsNullOrEmpty(d)));

                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);

                    Debug.Log($"[BizSim] Added {define} to {targetGroup}");
                }
            }
        }

        /// <summary>
        /// Remove a define symbol from the specified platforms.
        /// </summary>
        public static void RemoveDefine(string define, params BuildTargetGroup[] targetGroups)
        {
            foreach (var targetGroup in targetGroups)
            {
                if (targetGroup == BuildTargetGroup.Unknown) continue;

                NamedBuildTarget namedTarget = GetNamedBuildTarget(targetGroup);
                if (namedTarget == NamedBuildTarget.Unknown) continue;
                string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);

                var defineList = defines.Split(';').ToList();

                if (defineList.Contains(define))
                {
                    defineList.Remove(define);
                    string newDefines = string.Join(";", defineList.Where(d => !string.IsNullOrEmpty(d)));

                    PlayerSettings.SetScriptingDefineSymbols(namedTarget, newDefines);

                    Debug.Log($"[BizSim] Removed {define} from {targetGroup}");
                }
            }
        }

        /// <summary>
        /// Add BIZSIM_FIREBASE to all relevant platforms.
        /// </summary>
        public static void AddFirebaseDefineAllPlatforms()
        {
            AddDefine(FIREBASE_DEFINE, GetRelevantPlatforms());
        }

        /// <summary>
        /// Remove BIZSIM_FIREBASE from all platforms.
        /// </summary>
        public static void RemoveFirebaseDefineAllPlatforms()
        {
            RemoveDefine(FIREBASE_DEFINE, GetAllPlatforms());
        }

        /// <summary>
        /// Get relevant platforms (Android, iOS, Standalone).
        /// </summary>
        public static BuildTargetGroup[] GetRelevantPlatforms()
        {
            return new[]
            {
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.Standalone
            };
        }

        /// <summary>
        /// Get all non-unknown platforms.
        /// </summary>
        private static BuildTargetGroup[] GetAllPlatforms()
        {
            return new[]
            {
                BuildTargetGroup.Android,
                BuildTargetGroup.iOS,
                BuildTargetGroup.Standalone,
                BuildTargetGroup.WebGL
            };
        }

        /// <summary>
        /// Get platforms where BIZSIM_FIREBASE is currently defined.
        /// </summary>
        public static List<BuildTargetGroup> GetPlatformsWithFirebaseDefine()
        {
            return GetAllPlatforms()
                .Where(IsFirebaseDefinePresent)
                .ToList();
        }

        /// <summary>
        /// Convert BuildTargetGroup to NamedBuildTarget (Unity 2023+).
        /// </summary>
        private static NamedBuildTarget GetNamedBuildTarget(BuildTargetGroup targetGroup)
        {
            return targetGroup switch
            {
                BuildTargetGroup.Android => NamedBuildTarget.Android,
                BuildTargetGroup.iOS => NamedBuildTarget.iOS,
                BuildTargetGroup.Standalone => NamedBuildTarget.Standalone,
                BuildTargetGroup.WebGL => NamedBuildTarget.WebGL,
                _ => NamedBuildTarget.Unknown
            };
        }

        /// <summary>
        /// Get user-friendly status message for Firebase integration.
        /// </summary>
        public static string GetFirebaseStatusMessage(out MessageType messageType)
        {
            bool packageInstalled = IsFirebaseAnalyticsInstalled();
            bool definePresent = IsFirebaseDefinePresentAnywhere();

            if (packageInstalled && definePresent)
            {
                messageType = MessageType.Info;
                return "Firebase Analytics integration is active. Events will be logged automatically.";
            }
            else if (packageInstalled && !definePresent)
            {
                messageType = MessageType.Warning;
                return "Firebase Analytics is installed but BIZSIM_FIREBASE define is missing. Add it to enable analytics.";
            }
            else if (!packageInstalled && definePresent)
            {
                messageType = MessageType.Error;
                return "BIZSIM_FIREBASE is defined but Firebase Analytics is not installed. Remove the define or install the package.";
            }
            else
            {
                messageType = MessageType.None;
                return "Firebase Analytics is not configured. This is optional.";
            }
        }
    }
}
