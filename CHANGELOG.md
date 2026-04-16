# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
