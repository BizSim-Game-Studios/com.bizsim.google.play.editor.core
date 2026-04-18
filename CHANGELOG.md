# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.7.0] - 2026-04-18

### Added
- **EDM4U declared as `package.json` dependency** (`com.google.external-dependency-manager: 1.2.187`). Per `unity-package-standards.md` §"EDM4U as a declared `package.json` dependency", `editor.core` declares EDM4U for family consistency even though it has no Android side of its own. Consumers who add the OpenUPM scoped registry to their host project's `Packages/manifest.json` (one-time setup) will have Unity Package Manager auto-install EDM4U transitively. Installation section of README updated with the manifest snippet. With this release, the full `com.bizsim.google.play.*` family (7 packages) now declares EDM4U as a declared dep — the Open follow-up "EDM4U dep back-port" is closed.

### Fixed
- Missing `.meta` file for `Editor/PackageRegistry/PackageRemoveHandler.cs`.

## [1.6.0] - 2026-04-17

### Added
- **`FirebaseUpdater` one-click secure auto-updater (ADR-010, Plan H-3).** New `Editor/PackageRegistry/FirebaseUpdater.cs` implements the download → verify → extract → import pipeline previously deferred from 1.5.0. Dashboard's Firebase section now exposes an **Auto-Update Installed Modules** button alongside the existing **Download Manually** fallback. The updater (1) fetches the latest release metadata from `api.github.com/repos/firebase/firebase-unity-sdk/releases/latest`, (2) locates the `firebase_unity_sdk_*.zip` asset, (3) downloads it to `Temporary Cache/BizSimFirebaseUpdater/`, (4) verifies SHA256 against the release's `sha256sums.txt` when present — mismatch aborts with a visible error, and when the checksum file is absent the user is asked to accept TLS-only security (defaults to cancel), (5) extracts with a path-traversal guard that rejects any entry escaping the destination via `..` or absolute paths, and (6) invokes `AssetDatabase.ImportPackage(path, interactive: true)` per installed module so Unity shows its native import dialog before any file is written.
- **ADR-010 security controls:** URL allowlist (`github.com/firebase/firebase-unity-sdk/*`, `api.github.com/repos/firebase/firebase-unity-sdk/*`, `objects.githubusercontent.com/*`); HTTPS-only (no plain-HTTP fallback); interactive import (user reviews every file); temp-only writes under `Application.temporaryCachePath`; explicit user confirmation dialogs at download start and at each checksum-handling branch.
- `FirebaseUpdaterSecurityTest` drift guard (11 assertions): URL allowlist accepts/rejects expected origins including protocol injection, zip path-traversal guard detects `../` entries, SHA256 sums parser handles GNU two-space + single-space + comment + blank-line variants, GitHub asset JSON parser extracts name + download URL.

### Changed
- Dashboard's Firebase updates banner now renders two buttons when updates are available: **Auto-Update Installed Modules** (invokes `FirebaseUpdater`) and **Download Manually** (the previous behavior, retained for air-gapped environments or consumers whose corp policy forbids auto-import). Same no-update footer as 1.5.0.

## [1.5.0] - 2026-04-17

### Added
- **Per-module Firebase update detection (K9.4, Plan H-2).** `RemoteVersionChecker.PropagateFirebaseTagToEntries(registry)` copies the shared `LatestFirebaseTag` into every Firebase entry's `LatestTag` — both on cache hit and after a successful network check. This activates `PackageRegistryEntry.HasUpdate` per module: `DrawStatusDot(pkg.IsInstalled, entry.HasUpdate)` now lights up an orange dot on each Firebase module card whose installed version lags behind the latest upstream SDK release. Implementation rationale: Firebase Unity SDK ships as a single bundle, so all modules share one upstream version — per-module update detection is therefore a per-entry `HasUpdate` check against the same shared tag, no per-module CDN calls required.
- **Firebase updates banner in the dashboard.** When one or more installed modules have `HasUpdate == true`, the Firebase section surfaces an aggregate banner (`N Firebase module(s) have updates available (latest: vX.Y.Z)`) with a prominent **Download Latest SDK** button that opens the GitHub Releases page. When every installed module is current, the banner collapses to the informational `Latest Firebase Unity SDK release: vX.Y.Z` footer with `GitHub Releases` mini-link.
- `FirebaseTagPropagationTest` drift guard (5 assertions): propagation populates every entry, null registry is safe, empty tag leaves entries untouched, `HasUpdate` lights up when versions differ, `HasUpdate` stays false when versions match.
- `Editor/AssemblyInfo.cs` with `[InternalsVisibleTo]` for the `EditorTests` assembly. Enables test access to `internal` helpers like `RemoteVersionChecker.PropagateFirebaseTagToEntries` without widening the public API surface. Matches the pattern used across all `com.bizsim.google.play.*` bridge packages.

### Deferred
- **One-click Firebase auto-updater (originally scoped for 1.5.0, deferred to 1.6.0).** Automatic download + SHA256 verify + `AssetDatabase.ImportPackage` for installed modules requires security hardening (URL allowlist, zip path-traversal guards, checksum verification, user-confirmation dialog) — tracked under **ADR-010 (Firebase Updater Security)**. The manual download path remains the supported update flow until ADR-010 lands: click **Download Latest SDK**, extract the zip, re-import the `.unitypackage` files matching your installed modules. This scope split was made explicitly to avoid shipping a high-blast-radius installer without a proper security review.

## [1.4.0] - 2026-04-17

### Added
- **Firebase Performance Monitoring in `PackageRegistry.json` (K9.2, Plan H-1).** The registry now catalogs 13 active Firebase Unity SDK modules. Firebase Performance was previously missing; it is now detected, counted in the dashboard foldout (`X/13 modules`), and rendered in the per-module grid just like its siblings.
- `FirebaseRegistryCompletenessTest` drift guard (4 assertions): every active Firebase module present, unique `AssemblyName` values, `Firebase.*` namespace enforcement, correct `Category` assignment.

### Changed
- **Firebase dashboard section rewritten to per-module grid (K9.3, Plan H-1).** The top-of-section `Firebase SDK: vX.Y.Z` row has been removed; that row read only the Firebase Analytics version and mislabeled it as "the Firebase SDK version", which is inaccurate for projects running heterogeneous Firebase module versions. Per-module versions are now shown exclusively in the module grid below, each card reporting its own installed version parsed from `Assets/Firebase/Editor/{Module}_version-*.txt`. The `Firebase SDK update available` banner was likewise removed (it was Analytics-only); replaced with an informational `Latest Firebase Unity SDK release` footer linking to GitHub Releases. **Per-module update badges and a one-click secure update flow land in editor.core 1.5.0 (Plan H-2).**
- `Add/Remove BIZSIM_FIREBASE` buttons now enabled when ANY Firebase module is installed — not just Analytics. Projects that ship Firestore, Auth, or Remote Config without Analytics can now manage the define from the dashboard.

## [1.3.1] - 2026-04-17

### Changed
- **Dashboard label now reflects per-package native SDK family (K9 Plan G).** `BizSimPackageDashboard.cs:577` previously showed a hardcoded `"Play Core: {X}"` label for all BizSim packages — misleading because `games` uses Play Games Services (GMS) and `installreferrer` uses the Install Referrer Library (neither of which is "Play Core"). The label now reads from each package's `PackageVersion.NativeSdkLabel` const: `"Play Core (review)"`, `"Play Core (app-update)"`, `"Play Core (asset-delivery)"`, `"Play Core (age-signals beta)"`, `"Play Games Services v2"`, `"Install Referrer"` respectively.
- `PackageRegistryEntry.cs` added `NativeSdkArtifact` + `NativeSdkLabel` fields (K8 canonical). Legacy `PlayCoreArtifact` + `PlayCoreVersion` preserved as `[Obsolete]` aliases for backward-compatible JSON read (removed in editor.core 2.0.0 per ADR-009).
- `PackageDetector.EnrichWithVersionMetadata` prefers new `PackageVersion.NativeSdkVersion` const; falls back to legacy `PlayCoreVersion` / `PgsV2SdkVersion` during the transition window. Legacy `entry.PlayCoreVersion` mirrored from `entry.NativeSdkVersion` so any existing caller continues to work.

### Added
- `PackageRegistry.json` entries now include `NativeSdkArtifact` + `NativeSdkLabel` fields for all 6 bridge packages. Previously-empty artifact fields for `agesignals`, `games`, `installreferrer` are now populated.

## [1.3.0] - 2026-04-16

### Added
- Package Remove button (✕) with confirmation dialog showing dependent assemblies and scripting defines
- Firebase SDK version check via `firebase/firebase-unity-sdk` GitHub releases API
- Firebase update available banner with download link when newer SDK version detected
- Google Play Plugins (com.google.play.*) Install buttons via OpenUPM scoped registry
- Google Play Plugins latest release tag display with GitHub Releases link
- Platform section showing active build target, platform compatibility matrix, and Switch to Android button
- Context menu items for Firebase packages: Firebase Documentation, Firebase Console, Download Latest SDK
- Context menu items for Google Play packages: Google Play Developer Docs, Play Console
- Copy Package ID context menu item for all packages
- `PackageInstallQueue.CurrentRequest` property exposing the currently-installing package
- `PackageRemoveHandler` static class for UPM package removal with dependency analysis
- `RemoteVersionChecker.LatestFirebaseTag` and `LatestGooglePlayPluginsTag` static properties

### Changed
- Install/Update buttons now disable individually per-package during installation (previously global disable)
- Progress bar shows specific package name being installed instead of generic text
- Context menu (right-click / `...` button) now appears for ALL packages including uninstalled ones
- `ScopedRegistryConfigurator` now includes `com.google.play` in required OpenUPM scopes
- `PackageRegistryEntry` gains `ScopedRegistryInstall` field for OpenUPM-based packages
- `PackageRegistry.json` enriched: all Firebase packages now have `GitHubRepoName`, Google Play packages have `ScopedRegistryInstall`

## [1.2.0] - 2026-04-16

### Added
- `PackageRegistryEntry` metadata extension with `VersionClassName`, `SampleFolders`, `DocFiles`, `PlayCoreArtifact`, and `EditorInitDefine` fields
- Version, release date, and Play Core version display on package cards via reflection on each package's `PackageVersion` class
- Context menu (right-click or `...` button) per package card with: Open Configuration, View on GitHub, Copy Git Install URL, Import Sample submenu, Open Documentation submenu
- Header summary bar showing installed count and available updates
- Orange status dot for packages with available updates
- Professional card redesign with three-row layout: name+version, date+PlayCore, action buttons

### Changed
- `PackageRegistry.json` now includes full metadata for all BizSim packages (samples, docs, defines, version class names)
- Package card minimum width increased from 160 to 200 for improved readability
- `PackageDetector.ScanAll` now enriches registry entries with version metadata from `PackageVersion` classes

## [1.1.0] - 2026-04-16

### Added
- `PackageRegistryData` JSON-driven package registry replacing hardcoded ScanAll entries
- `ManifestReader` utility for reading installed package versions from Packages/manifest.json
- `RemoteVersionChecker` polling GitHub releases/latest API with 15-min SessionState cache
- `PackageInstallQueue` sequential UPM install processor via Client.Add
- `ScopedRegistryConfigurator` for idempotent OpenUPM scoped registry setup
- One-click **Install** button for not-installed BizSim packages in dashboard
- One-click **Update** button when newer GitHub release detected
- "Checking for updates..." indicator in toolbar during version check
- Install progress bar at bottom of dashboard during package operations

### Changed
- `PackageDetector.ScanAll()` now driven by `PackageRegistryData.json` instead of hardcoded entries
- Package versions enriched from manifest.json (accurate semver instead of "Installed")
- Dashboard buttons disabled during install operations

## [1.0.1] - 2026-04-16

### Added
- In-App Review, In-App Updates, Asset Delivery registered in `PackageDetector.ScanAll()` as `PackageCategory.BizSim`
- Figma Importer registered under new `PackageCategory.BizSimUtility` category
- Convenience detection methods: `IsReviewInstalled()`, `IsAppUpdateInstalled()`, `IsAssetDeliveryInstalled()`, `IsFigmaImporterInstalled()`

### Fixed
- Dashboard GC allocation: LINQ-filtered package lists now cached per `RefreshPackages()` call instead of per `OnGUI` repaint frame
- Dashboard refreshes on window focus (`OnFocus`) instead of requiring manual Refresh button

## [1.0.0] - 2026-04-14

### Added

- Initial release of `com.bizsim.google.play.editor.core` — shared editor utilities for the BizSim Google Play package family.
- `PackageDetector` — instant assembly-scan based package detection (no slow `Client.List()` calls).
- `BizSimDefineManager` — scripting define management for `BIZSIM_FIREBASE` across all build platforms (Android, iOS, Standalone, WebGL).
- `BizSimPackageDashboard` — unified Editor window showing the status of all BizSim and Google Play packages at a glance.

### Notes

- This is the first release under the new `com.bizsim.google.play.*` family naming. The previous incarnation (`com.bizsim.gplay.editor-core`) at version 0.1.4 is archived and no longer maintained.
- Floor: Unity 6.0 LTS (`6000.0`).
- Editor-only package — no Runtime asmdef.
