# BizSim Google Play — Editor Core

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.0.0-orange.svg)](CHANGELOG.md)

Shared editor utilities for all [BizSim Google Play](https://github.com/BizSim-Game-Studios) Unity packages. Provides package detection, scripting define management, and a unified dashboard window.

**Package:** `com.bizsim.google.play.editor.core`
**Namespace:** `BizSim.Google.Play.Editor.Core`
**Author:** [BizSim Game Studios](https://www.bizsim.com)
**License:** MIT

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Quick Start Tutorial](#quick-start-tutorial)
- [API Reference](#api-reference)
- [Used By](#used-by)
- [License](#license)
- [Requirements](#requirements)

## Overview

This package is the foundation for all BizSim Google Play packages. It solves three common problems:

1. **Package Detection** — Instantly detect whether Firebase, Google Play plugins, or other BizSim packages are installed, without slow `Client.List()` calls.
2. **Scripting Define Management** — Add or remove `BIZSIM_FIREBASE` across all build platforms with a single method call.
3. **Unified Dashboard** — A single Editor window that shows the status of all BizSim and Google Play packages at a glance.

### Architecture

```
BizSim.Google.Play.Editor.Core (this package)
├── PackageDetector        ← Assembly scanning (instant, non-blocking)
├── BizSimDefineManager    ← Scripting define CRUD for all platforms
└── BizSimPackageDashboard ← EditorWindow with package overview
```

All other BizSim packages reference this assembly for shared functionality:

```
com.bizsim.google.play.agesignals     ─┐
com.bizsim.google.play.installreferrer ─┤── reference ──→ com.bizsim.google.play.editor.core
com.bizsim.google.play.games           ─┘
```

## Installation

### Option 1: Git URL (recommended)

1. In Unity Editor: **Window > Package Manager > + > Add package from git URL...**
2. Enter:
   ```
   https://github.com/BizSim-Game-Studios/com.bizsim.google.play.editor.core.git
   ```

3. Or add directly to `Packages/manifest.json`:
   ```json
   "com.bizsim.google.play.editor.core": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.editor.core.git"
   ```

### Option 2: Local path

```json
"com.bizsim.google.play.editor.core": "file:../path/to/com.bizsim.google.play.editor.core"
```

## Quick Start Tutorial

### Step 1 — Open the Package Dashboard

```
Unity Menu → BizSim → Package Dashboard
```

The dashboard shows:

| Section | What it displays |
|---------|-----------------|
| Firebase Integration | Firebase Analytics install status, version, `BIZSIM_FIREBASE` define status |
| BizSim Packages | All `com.bizsim.*` packages with version and assembly status |
| Google Play Plugins | Official Google Play plugins (App Update, Asset Delivery, etc.) |
| Scripting Defines | Current define symbols across all build target groups |

### Step 2 — Enable Firebase integration

If Firebase Analytics is installed in your project, the dashboard detects it automatically. Click **"Add BIZSIM_FIREBASE"** to enable Firebase analytics logging in all BizSim packages.

You can also do this via code:

```csharp
using BizSim.Google.Play.Editor.Core;

// Check if Firebase Analytics package is installed
bool hasFirebase = BizSimDefineManager.IsFirebaseAnalyticsInstalled();
string version = BizSimDefineManager.GetFirebaseAnalyticsVersion();
Debug.Log($"Firebase Analytics: {hasFirebase} (v{version})");

// Add BIZSIM_FIREBASE define to all platforms
BizSimDefineManager.AddFirebaseDefineAllPlatforms();

// Remove it
BizSimDefineManager.RemoveFirebaseDefineAllPlatforms();
```

### Step 3 — Detect packages programmatically

Use `PackageDetector` to check if any assembly is loaded, without async UPM queries:

```csharp
using BizSim.Google.Play.Editor.Core;

// Check specific assemblies
bool hasAgeSignals = PackageDetector.IsAssemblyLoaded("BizSim.Google.Play.AgeSignals");
bool hasGames = PackageDetector.IsAssemblyLoaded("BizSim.Google.Play.Games");

// Get version of a loaded assembly
string version = PackageDetector.GetAssemblyVersion("BizSim.Google.Play.AgeSignals");

// Check Firebase specifically
bool hasFirebase = PackageDetector.IsFirebaseAnalyticsInstalled();
```

All checks are **instant** — they scan the current `AppDomain.GetAssemblies()` without any async operations or package manager queries.

### Step 4 — Manage scripting defines

`BizSimDefineManager` wraps Unity's `PlayerSettings.GetScriptingDefineSymbols` with a cleaner API:

```csharp
using BizSim.Google.Play.Editor.Core;

// Check if a define is present on any platform
bool isDefined = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();

// Get which platforms have the define
var platforms = BizSimDefineManager.GetPlatformsWithFirebaseDefine();
// Returns: ["Android", "iOS", "Standalone"]

// Get a human-readable status message for Editor UI
MessageType msgType;
string status = BizSimDefineManager.GetFirebaseStatusMessage(out msgType);
// Returns: "✓ Firebase Analytics detected (v12.5.0). BIZSIM_FIREBASE is active on: Android, iOS"
```

## API Reference

### PackageDetector

| Method | Returns | Description |
|--------|---------|-------------|
| `IsAssemblyLoaded(string name)` | `bool` | Check if a named assembly is in the current AppDomain |
| `GetAssemblyVersion(string name)` | `string` | Get the version of a loaded assembly (null if not found) |
| `IsFirebaseAnalyticsInstalled()` | `bool` | Shorthand for checking Firebase Analytics assembly |
| `GetFirebaseAnalyticsVersion()` | `string` | Get Firebase Analytics version string |

### BizSimDefineManager

| Method | Description |
|--------|-------------|
| `IsFirebaseAnalyticsInstalled()` | Delegates to `PackageDetector` |
| `GetFirebaseAnalyticsVersion()` | Delegates to `PackageDetector` |
| `IsFirebaseDefinePresentAnywhere()` | Check if `BIZSIM_FIREBASE` exists on any build target |
| `GetPlatformsWithFirebaseDefine()` | List of platform names where the define is active |
| `AddFirebaseDefineAllPlatforms()` | Add `BIZSIM_FIREBASE` to Android, iOS, and Standalone |
| `RemoveFirebaseDefineAllPlatforms()` | Remove `BIZSIM_FIREBASE` from all platforms |
| `GetFirebaseStatusMessage(out MessageType)` | Human-readable status for Editor UI |

### BizSimPackageDashboard

Open via menu: **BizSim > Package Dashboard**

No public API — this is an `EditorWindow` for visual package management.

## Used By

This package is a dependency of:

| Package | What it uses |
|---------|-------------|
| [Age Signals](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.agesignals) | `BIZSIM_FIREBASE` define for optional analytics |
| [Install Referrer](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer) | Configuration window with Firebase detection |
| [Games Services](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.games) | Package Dashboard integration |

## License

This package is licensed under the [MIT License](LICENSE.md) — Copyright (c) 2026 BizSim Game Studios.

This package does not include or depend on any third-party runtime libraries. It uses only Unity Editor APIs (`UnityEditor` namespace). See [NOTICES.md](NOTICES.md) for details.

## Requirements

- Unity 6000.0 LTS or later
- Editor-only package (no runtime code — excluded from builds)
