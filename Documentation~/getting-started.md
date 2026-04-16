# Getting Started

## Prerequisites

- Unity 6000.0 (Unity 6.0 LTS) or later
- No Android SDK or device required -- this is an editor-only package

## Installation

### Step 1 -- Add via Git URL

Open your Unity project's `Packages/manifest.json` and add:

```json
{
  "dependencies": {
    "com.bizsim.google.play.editor.core": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.editor.core.git#v1.2.0"
  }
}
```

Alternatively, use **Window > Package Manager > + > Add package from git URL** and paste:

```
https://github.com/BizSim-Game-Studios/com.bizsim.google.play.editor.core.git#v1.2.0
```

### Step 2 -- Verify installation

After Unity reimports, open **BizSim > Google Play > Package Dashboard**. The dashboard window should appear showing the list of detected BizSim packages.

## First Use

### Opening the Package Dashboard

1. Go to **BizSim > Google Play > Package Dashboard** in the Unity menu bar.
2. The dashboard scans loaded assemblies to detect installed BizSim packages.
3. Each row shows the package name, installed version, and status indicators for Firebase and EDM4U.

### Checking Package Detection

You can verify detection programmatically in an editor script:

```csharp
using BizSim.Google.Play.Editor.Core;

bool gamesInstalled = PackageDetector.IsAssemblyLoaded("BizSim.Google.Play.Games");
Debug.Log($"Games package installed: {gamesInstalled}");
```

### Managing Scripting Defines

The `BizSimDefineManager` lets sibling packages register their presence:

```csharp
using BizSim.Google.Play.Editor.Core;

// Add a define across all relevant platforms
BizSimDefineManager.AddDefine("BIZSIM_GAMES_INSTALLED",
    BizSimDefineManager.GetRelevantPlatforms());

// Check if Firebase is available
bool hasFirebase = BizSimDefineManager.IsFirebaseAnalyticsInstalled();
```

## Next Steps

- Read the [API Reference](api-reference.md) for the full public surface
- See [Architecture](architecture.md) for how editor.core fits into the package family
- Check [Troubleshooting](troubleshooting.md) if the dashboard does not detect a package
