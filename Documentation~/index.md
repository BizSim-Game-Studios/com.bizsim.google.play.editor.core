# BizSim Google Play Editor Core

Last reviewed: 2026-04-16

## Overview

BizSim Google Play Editor Core is the shared editor foundation for every `com.bizsim.google.play.*` Unity package. It provides three services that the sibling packages depend on:

1. **PackageDetector** -- assembly-scan-based detection of installed BizSim and third-party packages (Firebase, EDM4U, UniTask). Instant and non-blocking; never calls the slow `UnityEditor.PackageManager.Client.List()`.
2. **BizSimDefineManager** -- scripting-define CRUD that adds or removes symbols (`BIZSIM_FIREBASE`, `BIZSIM_<API>_INSTALLED`, etc.) across all relevant build platforms in a single call.
3. **BizSimPackageDashboard** -- a unified EditorWindow that lists every detected BizSim Google Play package with status, version, and quick-action buttons.

This package is editor-only. It has no Runtime assembly, no Android plugins, and no Samples.

## Table of Contents

| File | Description |
|------|-------------|
| [getting-started.md](getting-started.md) | Installation and first-use walkthrough |
| [api-reference.md](api-reference.md) | Full public API surface for PackageDetector, BizSimDefineManager, and BizSimPackageDashboard |
| [configuration.md](configuration.md) | Dashboard settings and customization options |
| [architecture.md](architecture.md) | Assembly structure, asmdef graph, and editor extensibility model |
| [troubleshooting.md](troubleshooting.md) | Common issues and fixes |
| [DATA_SAFETY.md](DATA_SAFETY.md) | Play Store Data Safety form guidance |

## Links

- [README](../README.md) -- quick-start experience
- [CHANGELOG](../CHANGELOG.md) -- version history
- [GitHub Repository](https://github.com/BizSim-Game-Studios/com.bizsim.google.play.editor.core)
