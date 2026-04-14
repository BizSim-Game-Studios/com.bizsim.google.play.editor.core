# Editor Core — Architecture

## Overview

`com.bizsim.google.play.editor.core` is an Editor-only shared utility package that provides:

1. **Package Detection** — instant assembly scanning via `AppDomain.GetAssemblies()`
2. **Define Management** — unified scripting define symbol management (`BIZSIM_FIREBASE`)
3. **Package Dashboard** — visual EditorWindow for package status overview

## Dependency Graph

```
BizSimPackageDashboard (EditorWindow)
├── PackageDetector (static, reads AppDomain)
└── BizSimDefineManager (static)
    └── PackageDetector
```

## Component Details

### PackageDetector (181 LOC)

Static utility class. Zero dependencies.

**Detection Strategy:**
- Scans `AppDomain.CurrentDomain.GetAssemblies()` for known assembly names
- Instant (no file I/O, no network) — safe to call in OnGUI
- Three-tier version detection: `InformationalVersion` → `FileVersion` → `Assembly.Version`
- Firebase manifest parsing for accurate version (regex on `_version-{V}_manifest.txt` files)

**Detected Packages:**
| Category | Packages |
|----------|----------|
| Firebase | Analytics, Auth, Crashlytics, RemoteConfig, Messaging, Storage, Firestore, Functions, Database |
| BizSim | AgeSignals, Games, InstallReferrer |
| Google Play | Common, Core, AppUpdate, AssetDelivery |

### BizSimDefineManager (235 LOC)

Static utility class. Depends on PackageDetector.

**Key Responsibilities:**
- Single source of truth for `BIZSIM_FIREBASE` define symbol
- Cross-platform support: Android, iOS, Standalone
- Unity 2023.1+ API compatibility via `#if UNITY_2023_1_OR_NEWER`

**API Pattern:**
```csharp
// Check
BizSimDefineManager.IsDefinePresent("BIZSIM_FIREBASE", BuildTargetGroup.Android);

// Modify
BizSimDefineManager.AddDefine("BIZSIM_FIREBASE", BuildTargetGroup.Android, BuildTargetGroup.iOS);
BizSimDefineManager.RemoveDefine("BIZSIM_FIREBASE", BuildTargetGroup.Android);

// Bulk
BizSimDefineManager.AddFirebaseDefineAllPlatforms();
```

### BizSimPackageDashboard (349 LOC)

EditorWindow with menu entry at `BizSim > Package Dashboard`.

**Sections:**
1. **Toolbar** — title + refresh button
2. **Firebase** — module count, version, define status, add/remove buttons
3. **BizSim Packages** — installation grid
4. **Google Play Plugins** — installation grid
5. **Define Symbols** — per-platform define status with toggle buttons

**Refresh Cycle:**
1. User clicks "Refresh" or window opens
2. `PackageDetector.ScanAll()` scans AppDomain (~16 packages, <1ms)
3. Results cached in `_packages` list
4. `OnGUI()` renders from cache

## Assembly Definition

```
BizSim.Google.Play.Editor.Core
├── Platform: Editor only
├── References: none
├── autoReferenced: true
└── noEngineReferences: false
```

## Consumed By

- `com.bizsim.google.play.agesignals` (Editor assembly)
- `com.bizsim.google.play.games` (Editor assembly)
- `com.bizsim.google.play.installreferrer` (Editor assembly)
