# Editor Core — API Reference

## PackageDetector

```csharp
namespace BizSim.Google.Play.Editor.Core

public static class PackageDetector
{
    // Assembly detection
    static bool IsAssemblyLoaded(string assemblyName);
    static string GetAssemblyVersion(string assemblyName);

    // Firebase shortcuts
    static bool IsFirebaseAnalyticsInstalled();
    static bool IsFirebaseAuthInstalled();
    static bool IsFirebaseCrashlyticsInstalled();
    static bool IsFirebaseRemoteConfigInstalled();
    static bool IsFirebaseMessagingInstalled();
    static bool IsFirebaseStorageInstalled();
    static bool IsFirebaseFirestoreInstalled();
    static bool IsFirebaseFunctionsInstalled();
    static bool IsFirebaseDatabaseInstalled();
    static string GetFirebaseAnalyticsVersion();
    static string GetFirebaseModuleVersion(string moduleName);

    // BizSim shortcuts
    static bool IsAgeSignalsInstalled();
    static bool IsGamesServicesInstalled();
    static bool IsInstallReferrerInstalled();

    // Google Play shortcuts
    static bool IsGooglePlayCommonInstalled();
    static bool IsGooglePlayCoreInstalled();
    static bool IsGooglePlayAppUpdateInstalled();
    static bool IsGooglePlayAssetDeliveryInstalled();

    // Bulk scan
    static List<PackageInfo> ScanAll();
}

public struct PackageInfo
{
    public string DisplayName;
    public string AssemblyName;
    public PackageCategory Category;
    public bool IsInstalled;
    public string Version;
}

public enum PackageCategory { Firebase, BizSim, GooglePlay }
```

## BizSimDefineManager

```csharp
namespace BizSim.Google.Play.Editor.Core

public static class BizSimDefineManager
{
    const string FIREBASE_DEFINE = "BIZSIM_FIREBASE";

    // Firebase integration
    static bool IsFirebaseAnalyticsInstalled();
    static string GetFirebaseAnalyticsVersion();

    // Define management
    static bool IsDefinePresent(string define, BuildTargetGroup targetGroup);
    static void AddDefine(string define, params BuildTargetGroup[] targetGroups);
    static void RemoveDefine(string define, params BuildTargetGroup[] targetGroups);

    // Firebase define shortcuts
    static bool IsFirebaseDefinePresent(BuildTargetGroup targetGroup);
    static bool IsFirebaseDefinePresentAnywhere();
    static void AddFirebaseDefineAllPlatforms();
    static void RemoveFirebaseDefineAllPlatforms();

    // Platform info
    static BuildTargetGroup[] GetRelevantPlatforms();
    static List<BuildTargetGroup> GetPlatformsWithFirebaseDefine();

    // Status
    static string GetFirebaseStatusMessage(out MessageType messageType);
}
```

## BizSimPackageDashboard

```csharp
namespace BizSim.Google.Play.Editor.Core

// Menu: BizSim > Package Dashboard
public class BizSimPackageDashboard : EditorWindow
{
    static void ShowWindow();  // Opens or focuses the dashboard
}
```

No public API beyond the menu item. All interaction is through the EditorWindow GUI.
