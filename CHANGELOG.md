# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
