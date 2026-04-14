using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Detects installed packages by checking loaded assemblies in the current AppDomain.
    /// Works for both UPM packages and asset-imported (.unitypackage) DLLs.
    /// All checks are instant and non-blocking.
    /// </summary>
    public static class PackageDetector
    {
        /// <summary>
        /// Check if a specific assembly is loaded in the current AppDomain.
        /// </summary>
        public static bool IsAssemblyLoaded(string assemblyName)
        {
            return System.AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == assemblyName);
        }

        /// <summary>
        /// Get the version of a loaded assembly.
        /// </summary>
        public static string GetAssemblyVersion(string assemblyName)
        {
            var assembly = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == assemblyName);

            if (assembly == null) return null;

            // Priority 1: AssemblyInformationalVersion (Firebase stores real version here)
            var infoAttr = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false);
            if (infoAttr.Length > 0)
            {
                string infoVersion = ((System.Reflection.AssemblyInformationalVersionAttribute)infoAttr[0]).InformationalVersion;
                if (!string.IsNullOrEmpty(infoVersion) && infoVersion != "0.0.0.0")
                    return infoVersion;
            }

            // Priority 2: AssemblyFileVersion
            var fileAttr = assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyFileVersionAttribute), false);
            if (fileAttr.Length > 0)
            {
                string fileVersion = ((System.Reflection.AssemblyFileVersionAttribute)fileAttr[0]).Version;
                if (!string.IsNullOrEmpty(fileVersion) && fileVersion != "0.0.0.0")
                    return fileVersion;
            }

            // Priority 3: AssemblyVersion (often 0.0.0.0 for Firebase)
            var version = assembly.GetName().Version;
            if (version != null && version.ToString() != "0.0.0.0")
                return version.ToString();

            return "Installed";
        }

        // --- Firebase ---

        public static bool IsFirebaseAnalyticsInstalled() => IsAssemblyLoaded("Firebase.Analytics");
        public static bool IsFirebaseAuthInstalled() => IsAssemblyLoaded("Firebase.Auth");
        public static bool IsFirebaseCrashlyticsInstalled() => IsAssemblyLoaded("Firebase.Crashlytics");
        public static bool IsFirebaseRemoteConfigInstalled() => IsAssemblyLoaded("Firebase.RemoteConfig");
        public static bool IsFirebaseMessagingInstalled() => IsAssemblyLoaded("Firebase.Messaging");
        public static bool IsFirebaseStorageInstalled() => IsAssemblyLoaded("Firebase.Storage");
        public static bool IsFirebaseFirestoreInstalled() => IsAssemblyLoaded("Firebase.Firestore");
        public static bool IsFirebaseFunctionsInstalled() => IsAssemblyLoaded("Firebase.Functions");
        public static bool IsFirebaseDatabaseInstalled() => IsAssemblyLoaded("Firebase.Database");

        /// <summary>
        /// Get Firebase Analytics version from manifest file in Assets/Firebase/Editor/.
        /// Firebase DLLs have version 0.0.0.0, so we parse the manifest filename instead.
        /// Pattern: FirebaseAnalytics_version-{VERSION}_manifest.txt
        /// </summary>
        public static string GetFirebaseAnalyticsVersion()
        {
            return GetFirebaseModuleVersion("FirebaseAnalytics");
        }

        /// <summary>
        /// Get any Firebase module version from its manifest file.
        /// </summary>
        public static string GetFirebaseModuleVersion(string moduleName)
        {
            // Search for manifest files: {ModuleName}_version-{VERSION}_manifest.txt
            string[] guids = AssetDatabase.FindAssets(moduleName + "_version", new[] { "Assets/Firebase/Editor" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                // Parse: FirebaseAnalytics_version-13.7.0_manifest
                var match = Regex.Match(fileName, @"_version-(.+?)_manifest");
                if (match.Success)
                    return match.Groups[1].Value;
            }

            // Fallback to assembly version
            string assemblyVersion = GetAssemblyVersion("Firebase." + moduleName.Replace("Firebase", ""));
            return assemblyVersion ?? "Installed";
        }

        // --- BizSim Packages ---

        public static bool IsAgeSignalsInstalled() => IsAssemblyLoaded("BizSim.Google.Play.AgeSignals");
        public static bool IsGamesServicesInstalled() => IsAssemblyLoaded("BizSim.Google.Play.Games");
        public static bool IsInstallReferrerInstalled() => IsAssemblyLoaded("BizSim.Google.Play.InstallReferrer");

        // --- Google Play Plugins ---

        public static bool IsGooglePlayCommonInstalled() => IsAssemblyLoaded("Google.Play.Common");
        public static bool IsGooglePlayCoreInstalled() => IsAssemblyLoaded("Google.Play.Core");
        public static bool IsGooglePlayAppUpdateInstalled() => IsAssemblyLoaded("Google.Play.AppUpdate");
        public static bool IsGooglePlayAssetDeliveryInstalled() => IsAssemblyLoaded("Google.Play.AssetDelivery");

        /// <summary>
        /// Scan all known packages and return their status.
        /// Instant, non-blocking.
        /// </summary>
        public static List<PackageInfo> ScanAll()
        {
            var results = new List<PackageInfo>();

            // Firebase
            AddDetection(results, "Firebase Analytics", "Firebase.Analytics", PackageCategory.Firebase);
            AddDetection(results, "Firebase Auth", "Firebase.Auth", PackageCategory.Firebase);
            AddDetection(results, "Firebase Crashlytics", "Firebase.Crashlytics", PackageCategory.Firebase);
            AddDetection(results, "Firebase Remote Config", "Firebase.RemoteConfig", PackageCategory.Firebase);
            AddDetection(results, "Firebase Messaging", "Firebase.Messaging", PackageCategory.Firebase);
            AddDetection(results, "Firebase Storage", "Firebase.Storage", PackageCategory.Firebase);
            AddDetection(results, "Firebase Firestore", "Firebase.Firestore", PackageCategory.Firebase);
            AddDetection(results, "Firebase Functions", "Firebase.Functions", PackageCategory.Firebase);
            AddDetection(results, "Firebase Database", "Firebase.Database", PackageCategory.Firebase);

            // BizSim
            AddDetection(results, "Age Signals", "BizSim.Google.Play.AgeSignals", PackageCategory.BizSim);
            AddDetection(results, "Games Services", "BizSim.Google.Play.Games", PackageCategory.BizSim);
            AddDetection(results, "Install Referrer", "BizSim.Google.Play.InstallReferrer", PackageCategory.BizSim);

            // Google Play
            AddDetection(results, "Play Common", "Google.Play.Common", PackageCategory.GooglePlay);
            AddDetection(results, "Play Core", "Google.Play.Core", PackageCategory.GooglePlay);
            AddDetection(results, "App Update", "Google.Play.AppUpdate", PackageCategory.GooglePlay);
            AddDetection(results, "Asset Delivery", "Google.Play.AssetDelivery", PackageCategory.GooglePlay);

            return results;
        }

        private static void AddDetection(List<PackageInfo> list, string displayName, string assemblyName, PackageCategory category)
        {
            bool loaded = IsAssemblyLoaded(assemblyName);
            list.Add(new PackageInfo
            {
                DisplayName = displayName,
                AssemblyName = assemblyName,
                Category = category,
                IsInstalled = loaded,
                Version = loaded ? GetAssemblyVersion(assemblyName) : null
            });
        }
    }

    public struct PackageInfo
    {
        public string DisplayName;
        public string AssemblyName;
        public PackageCategory Category;
        public bool IsInstalled;
        public string Version;
    }

    public enum PackageCategory
    {
        Firebase,
        BizSim,
        GooglePlay
    }
}
