# Troubleshooting

## Problem: Package Dashboard shows "Not Installed" for a package that is imported

**Cause:** The package's assembly failed to compile, or its asmdef has a `defineConstraint` that is not satisfied. Unity does not load assemblies that fail compilation.

**Fix:**
1. Check the Console window for compilation errors in the affected package.
2. Verify the package's asmdef `includePlatforms` matches the current editor context.
3. Click **Refresh** in the dashboard after resolving errors.

---

## Problem: Scripting define is not applied after installing a sibling package

**Cause:** The sibling package's `[InitializeOnLoad]` constructor did not run. This can happen if the editor skipped domain reload (e.g., "Enter Play Mode Options" with domain reload disabled).

**Fix:**
1. Trigger a domain reload: **Edit > Preferences > General > Script Changes While Playing > Recompile And Continue Playing**, then make a trivial script change.
2. Alternatively, restart the Unity Editor.
3. Verify the define is present: **Edit > Project Settings > Player > Other Settings > Scripting Define Symbols**.

---

## Problem: Firebase detection returns false even though Firebase is installed

**Cause:** `PackageDetector.IsFirebaseAnalyticsInstalled()` looks for the `Firebase.Analytics` assembly by name. If Firebase was installed via a `.unitypackage` that uses a different assembly name, detection fails.

**Fix:**
1. Verify the Firebase assembly name: open **Window > Analysis > Assembly Definition References** and search for `Firebase`.
2. If the assembly is named differently, this is a known limitation. File an issue on the editor.core repository.

---

## Problem: BizSimDefineManager.AddDefine does not persist across editor restarts

**Cause:** This is expected behavior. Defines set via `PlayerSettings.SetScriptingDefineSymbols` persist in `ProjectSettings/ProjectSettings.asset`. If that file is not committed or is overwritten by version control, defines are lost.

**Fix:**
1. Commit `ProjectSettings/ProjectSettings.asset` to version control after initial setup.
2. Each sibling package's `[InitializeOnLoad]` re-applies its define on every domain reload, so defines are self-healing on editor restart.

---

## Problem: Package Dashboard menu item is missing

**Cause:** The `BizSim.Google.Play.Editor.Core` assembly did not compile. This usually means the package is not properly installed or there is a version conflict.

**Fix:**
1. Check **Window > Package Manager** to confirm the package appears in the list.
2. Look for compilation errors referencing `BizSim.Google.Play.Editor.Core` in the Console.
3. Re-import the package: remove the entry from `manifest.json`, let Unity reimport, then add it back.
