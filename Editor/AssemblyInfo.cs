using System.Runtime.CompilerServices;

// Exposes internal test hooks (e.g. RemoteVersionChecker.PropagateFirebaseTagToEntries,
// PackageVersion.Current if/when added) to the EditorTests assembly without widening the
// public API surface. Matches the InternalsVisibleTo pattern used across the
// com.bizsim.google.play.* bridge packages.
[assembly: InternalsVisibleTo("BizSim.Google.Play.Editor.Core.EditorTests")]
