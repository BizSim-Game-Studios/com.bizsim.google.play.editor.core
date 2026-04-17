# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
