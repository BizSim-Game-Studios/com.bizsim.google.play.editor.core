# BizSim Google Play Editor Core

[![Unity 6000.0+](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.7.0-orange.svg)](CHANGELOG.md)

Shared editor utilities for all `com.bizsim.google.play.*` Unity packages.
Provides assembly-based package detection, scripting define management, and a unified dashboard window — entirely at editor time, with no runtime footprint.

## Features

- **Package detection** — Instant `AppDomain.GetAssemblies()` scan to detect Firebase, BizSim, and Google Play packages without slow async `Client.List()` calls
- **Scripting define management** — Add or remove `BIZSIM_FIREBASE` and per-package `BIZSIM_*_INSTALLED` defines across all build target groups with a single call
- **Unified dashboard** — `BizSimPackageDashboard` EditorWindow shows install status, versions, and define state for every package in the family at a glance
- **Editor-only** — No runtime assembly; excluded from Android, iOS, and Standalone builds entirely

## Installation

Edit `Packages/manifest.json` and add this package as a Git URL:

```json
{
  "dependencies": {
    "com.bizsim.google.play.editor.core": "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.editor.core.git#v1.7.0"
  }
}
```

> **Note:** If you are also installing any other `com.bizsim.google.play.*` package, add the OpenUPM scoped registry for `com.google.external-dependency-manager` to your `manifest.json` as well — those packages resolve their Android Maven deps through EDM4U. This package itself has no Android dependencies and does not require EDM4U.

## Quick Start

### Open the Package Dashboard

```
Unity Menu → BizSim → Package Dashboard
```

The dashboard shows:

| Section | What it displays |
|---------|-----------------|
| Firebase Integration | Firebase Analytics install status, version, `BIZSIM_FIREBASE` define state |
| BizSim Packages | All `com.bizsim.*` packages with version and assembly status |
| Google Play Plugins | Official Google Play plugins (App Update, Asset Delivery, etc.) |
| Scripting Defines | Current define symbols across all build target groups |

### Enable Firebase integration

If Firebase Analytics is installed, the dashboard detects it automatically. Click **"Add BIZSIM\_FIREBASE"** or call:

```csharp
using BizSim.Google.Play.Editor.Core;

BizSimDefineManager.AddFirebaseDefineAllPlatforms();
```

### Detect packages programmatically

```csharp
using BizSim.Google.Play.Editor.Core;

bool hasAgeSignals = PackageDetector.IsAssemblyLoaded("BizSim.Google.Play.AgeSignals");
string version     = PackageDetector.GetAssemblyVersion("BizSim.Google.Play.AgeSignals");
bool hasFirebase   = PackageDetector.IsFirebaseAnalyticsInstalled();
```

All checks are instant — no async operations, no Package Manager queries.

### Manage scripting defines

```csharp
using BizSim.Google.Play.Editor.Core;

bool isDefined = BizSimDefineManager.IsFirebaseDefinePresentAnywhere();
var platforms  = BizSimDefineManager.GetPlatformsWithFirebaseDefine();
// Returns: ["Android", "iOS", "Standalone"]

MessageType msgType;
string status = BizSimDefineManager.GetFirebaseStatusMessage(out msgType);
// "✓ Firebase Analytics detected (v12.5.0). BIZSIM_FIREBASE is active on: Android, iOS"
```

## API Reference

### PackageDetector

| Method | Returns | Description |
|--------|---------|-------------|
| `IsAssemblyLoaded(string name)` | `bool` | Check if a named assembly is in the current AppDomain |
| `GetAssemblyVersion(string name)` | `string` | Version of a loaded assembly (`null` if not found) |
| `IsFirebaseAnalyticsInstalled()` | `bool` | Shorthand for checking the Firebase Analytics assembly |
| `GetFirebaseAnalyticsVersion()` | `string` | Firebase Analytics version string |

### BizSimDefineManager

| Method | Description |
|--------|-------------|
| `IsFirebaseAnalyticsInstalled()` | Delegates to `PackageDetector` |
| `GetFirebaseAnalyticsVersion()` | Delegates to `PackageDetector` |
| `IsFirebaseDefinePresentAnywhere()` | Check if `BIZSIM_FIREBASE` exists on any build target |
| `GetPlatformsWithFirebaseDefine()` | List of platform names where the define is active |
| `AddFirebaseDefineAllPlatforms()` | Add `BIZSIM_FIREBASE` to Android, iOS, and Standalone |
| `RemoveFirebaseDefineAllPlatforms()` | Remove `BIZSIM_FIREBASE` from all platforms |
| `GetFirebaseStatusMessage(out MessageType)` | Human-readable status string for Editor UI |

### BizSimPackageDashboard

Open via menu: **BizSim > Package Dashboard**

No public API — this is an `EditorWindow` for visual package management.

## Used By

Every `com.bizsim.google.play.*` package references this assembly for shared editor functionality:

| Package | What it uses |
|---------|-------------|
| [Age Signals](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.agesignals) | `BIZSIM_FIREBASE` define, Configuration window Firebase detection |
| [App Update](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.appupdate) | `BIZSIM_APPUPDATE_INSTALLED` define, Configuration window |
| [Asset Delivery](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.assetdelivery) | `BIZSIM_ASSETDELIVERY_INSTALLED` define, Configuration window |
| [Games Services](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.games) | Package Dashboard integration, `BIZSIM_GAMES_INSTALLED` define |
| [Install Referrer](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.installreferrer) | Configuration window with Firebase detection |
| [In-App Review](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.review) | `BIZSIM_REVIEW_INSTALLED` define, Configuration window |

## Requirements

- Unity 6000.0 LTS or later
- Editor-only package — no runtime code, excluded from all player builds

## License

This package is licensed under the [MIT License](LICENSE.md) — Copyright (c) 2026 BizSim Game Studios.

This package bundles no third-party SDK binaries. It uses only Unity Editor APIs (`UnityEditor` namespace). See [NOTICES.md](NOTICES.md) for details.
