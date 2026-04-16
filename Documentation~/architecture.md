# Architecture

## Package Role

editor.core is the shared editor dependency for the entire `com.bizsim.google.play.*` family. Every sibling package's Editor asmdef references `BizSim.Google.Play.Editor.Core`. This avoids duplication of package detection logic, define management, and dashboard UI across the family.

## Assembly Structure

```
BizSim.Google.Play.Editor.Core (Editor asmdef)
    includePlatforms: [Editor]
    references: (none -- this is the root of the editor dependency tree)
```

There is no Runtime asmdef. All code compiles only in the Unity Editor.

## Dependency Graph

```
com.bizsim.google.play.agesignals.Editor ──┐
com.bizsim.google.play.games.Editor ────────┤
com.bizsim.google.play.installreferrer.Editor ──┤
com.bizsim.google.play.review.Editor ───────┤──> BizSim.Google.Play.Editor.Core
com.bizsim.google.play.appupdate.Editor ────┤
com.bizsim.google.play.assetdelivery.Editor ┘
```

Each sibling's Editor asmdef lists `BizSim.Google.Play.Editor.Core` in its `references` array.

## Component Responsibilities

### PackageDetector

- Scans `AppDomain.CurrentDomain.GetAssemblies()` for known BizSim assembly names
- Returns `PackageInfo` structs with name, version, and install status
- Hardcoded `ScanAll()` list -- new packages must be registered here
- Checks `AssemblyInformationalVersionAttribute` first, falls back to `AssemblyName.Version`
- Also detects Firebase Analytics and EDM4U assemblies

### BizSimDefineManager

- Wraps `PlayerSettings.GetScriptingDefineSymbols` / `SetScriptingDefineSymbols` (via `NamedBuildTarget`)
- Applies defines across multiple `BuildTargetGroup` values in one call
- Sibling packages call `AddDefine` from their `[InitializeOnLoad]` constructors
- Provides Firebase status queries that delegate to `PackageDetector`

### BizSimPackageDashboard

- `EditorWindow` opened from the BizSim menu
- Calls `PackageDetector.ScanAll()` on open and on refresh
- Renders a table of packages with status indicators
- Read-only dashboard -- does not mutate package state

## Adding a New Package to the Dashboard

1. Open `PackageDetector.cs`
2. Add the new assembly name to the `ScanAll()` hardcoded list
3. Bump editor.core's version (this is a `feat` change)
4. All sibling packages that update their editor.core dependency will see the new entry

## Thread Model

All code runs on the Unity main thread. There are no background threads, async operations, or JNI calls. `PackageDetector.ScanAll()` is synchronous and completes in microseconds because it reads from the already-loaded assembly list.
