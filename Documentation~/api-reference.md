# API Reference

Namespace: `BizSim.Google.Play.Editor.Core`

Assembly: `BizSim.Google.Play.Editor.Core`

All types in this package are editor-only. They are available in custom editor scripts, editor windows, and build processors.

---

## PackageDetector

`public static class PackageDetector`

Detects installed packages by scanning loaded assemblies in the current AppDomain. Instant and non-blocking -- does not use `UnityEditor.PackageManager.Client.List()`.

### Methods

| Method | Return | Description |
|--------|--------|-------------|
| `IsAssemblyLoaded(string assemblyName)` | `bool` | Returns `true` if the named assembly is loaded |
| `GetAssemblyVersion(string assemblyName)` | `string` | Returns the informational or assembly version, or `null` if not found |
| `IsFirebaseAnalyticsInstalled()` | `bool` | Checks for `Firebase.Analytics` assembly |
| `GetFirebaseAnalyticsVersion()` | `string` | Returns Firebase Analytics version string, or `null` |
| `ScanAll()` | `List<PackageInfo>` | Scans all known BizSim package assemblies and returns their status |

### PackageInfo

`public readonly struct PackageInfo`

| Property | Type | Description |
|----------|------|-------------|
| `AssemblyName` | `string` | The asmdef name (e.g., `BizSim.Google.Play.Games`) |
| `DisplayName` | `string` | Human-readable name |
| `IsInstalled` | `bool` | Whether the assembly is loaded |
| `Version` | `string` | Detected version, or `null` |

---

## BizSimDefineManager

`public static class BizSimDefineManager`

Manages scripting define symbols for all BizSim Google Play packages. Provides add, remove, and query operations that apply across multiple build platforms in a single call.

### Constants

| Constant | Value | Description |
|----------|-------|-------------|
| `FIREBASE_DEFINE` | `"BIZSIM_FIREBASE"` | The define symbol for Firebase Analytics availability |

### Methods

| Method | Return | Description |
|--------|--------|-------------|
| `IsFirebaseAnalyticsInstalled()` | `bool` | Delegates to `PackageDetector.IsFirebaseAnalyticsInstalled()` |
| `GetFirebaseAnalyticsVersion()` | `string` | Delegates to `PackageDetector.GetFirebaseAnalyticsVersion()` |
| `IsDefinePresent(string define, BuildTargetGroup group)` | `bool` | Checks if a symbol is defined for the given build target |
| `AddDefine(string define, BuildTargetGroup[] groups)` | `void` | Adds a define across the specified platforms |
| `RemoveDefine(string define, BuildTargetGroup[] groups)` | `void` | Removes a define across the specified platforms |
| `GetRelevantPlatforms()` | `BuildTargetGroup[]` | Returns the set of platforms relevant to BizSim packages (Android + Standalone) |

---

## BizSimPackageDashboard

`public class BizSimPackageDashboard : EditorWindow`

Unified editor window that lists all detected BizSim Google Play packages. Opened via **BizSim > Google Play > Package Dashboard**.

### Menu Path

`BizSim/Google Play/Package Dashboard`

### Behavior

- On open, calls `PackageDetector.ScanAll()` to populate the package list
- Displays each package with name, version, and installation status
- Shows Firebase Analytics detection status
- Provides refresh button to re-scan assemblies
