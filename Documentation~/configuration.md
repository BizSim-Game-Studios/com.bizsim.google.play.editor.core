# Configuration

## Package Dashboard

The BizSim Package Dashboard is the primary configuration surface for editor.core. Open it via **BizSim > Google Play > Package Dashboard**.

### Dashboard Panels

The dashboard displays:

1. **Package List** -- all known BizSim Google Play packages with detection status
2. **Firebase Status** -- whether `com.google.firebase.analytics` is installed and its version
3. **EDM4U Status** -- whether External Dependency Manager for Unity is detected

### Refreshing Detection

Click the **Refresh** button to re-scan loaded assemblies. This is useful after importing a new BizSim package or removing one.

## Scripting Define Symbols

editor.core manages the following defines on behalf of sibling packages:

| Define | Set When |
|--------|----------|
| `BIZSIM_FIREBASE` | `com.google.firebase.analytics` is installed (via `versionDefines` in each Runtime asmdef) |
| `BIZSIM_AGESIGNALS_INSTALLED` | `com.bizsim.google.play.agesignals` is imported |
| `BIZSIM_GAMES_INSTALLED` | `com.bizsim.google.play.games` is imported |
| `BIZSIM_INSTALLREFERRER_INSTALLED` | `com.bizsim.google.play.installreferrer` is imported |
| `BIZSIM_REVIEW_INSTALLED` | `com.bizsim.google.play.review` is imported |
| `BIZSIM_APPUPDATE_INSTALLED` | `com.bizsim.google.play.appupdate` is imported |
| `BIZSIM_ASSETDELIVERY_INSTALLED` | `com.bizsim.google.play.assetdelivery` is imported |

Each sibling package's `[InitializeOnLoad]` static constructor calls `BizSimDefineManager.AddDefine()` to register its define. Uninstalling a package leaves the define orphaned until the next editor restart or manual removal.

## Build Target Platforms

`BizSimDefineManager.GetRelevantPlatforms()` returns the set of `BuildTargetGroup` values that BizSim packages care about. Currently this includes:

- `BuildTargetGroup.Android`
- `BuildTargetGroup.Standalone`

Defines are applied to all platforms in this set to ensure consistent compilation behavior in the editor regardless of the active build target.

## No Settings Asset

Unlike the sibling `google.play.*` packages, editor.core does not ship a `*Settings` ScriptableObject. Configuration is implicit: the dashboard reads from `PackageDetector.ScanAll()` and `BizSimDefineManager` state at display time.
