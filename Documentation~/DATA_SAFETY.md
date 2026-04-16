# Data Safety

## Play Store Data Safety Form Guidance

This document describes what data flows through `com.bizsim.google.play.editor.core` to help consumers fill out the Google Play Store Data Safety form.

## Data Collection

**This package does NOT collect, persist, or transmit any data.**

editor.core is an editor-only package. It does not ship any code in Android player builds. Its assemblies are excluded from the build via `includePlatforms: ["Editor"]` in the asmdef definition.

## Runtime Impact

- No classes from this package exist in the compiled APK/AAB
- No network calls are made
- No data is written to disk on the player device
- No analytics events are sent

## Play Store Form

When filling out the Play Store Data Safety form, you do **not** need to declare anything for this package. It has zero runtime presence.

## Sibling Packages

Each sibling `com.bizsim.google.play.*` package ships its own `Documentation~/DATA_SAFETY.md` with package-specific data flow declarations. Consult those individually.
