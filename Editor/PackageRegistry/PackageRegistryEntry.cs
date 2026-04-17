using System;

namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Describes a single package in the BizSim ecosystem registry.
    /// Serializable for JSON loading; transient fields populated at runtime.
    /// </summary>
    [Serializable]
    public class PackageRegistryEntry
    {
        public string PackageId;
        public string DisplayName;
        public string AssemblyName;
        public string GitRepoUrl;
        public string GitHubRepoName;
        public string ConfigWindowTypeName;
        public PackageCategory Category;

        // Dashboard metadata (populated from PackageRegistry.json)
        public string VersionClassName;
        public string[] SampleFolders;
        public string[] DocFiles;
        // K8/K9 canonical native SDK metadata (Plan G).
        // Per 06-conventions/06-package-version-schema.md +
        // 07-dashboard-metadata-contract.md in enterprise-quality-bar meta-spec.
        public string NativeSdkArtifact;  // Maven coord, e.g. "com.google.android.play:review:2.0.2"
        public string NativeSdkLabel;     // Dashboard display label, e.g. "Play Core (review)"

        // Legacy alias — kept temporarily for backward-compatible JSON read.
        // Removed in editor.core 2.0.0 once all BizSim packages have migrated.
        [Obsolete("Use NativeSdkArtifact. Removed in editor.core 2.0.0 per ADR-009.", error: false)]
        public string PlayCoreArtifact;

        public string EditorInitDefine;
        public bool ScopedRegistryInstall;

        [NonSerialized] public string LatestTag;
        [NonSerialized] public string InstalledVersion;
        [NonSerialized] public bool IsInstalled;

        // Version metadata (populated at scan time via reflection on VersionClassName)
        [NonSerialized] public string CurrentVersion;
        [NonSerialized] public string ReleaseDate;
        [NonSerialized] public string NativeSdkVersion;  // K8 canonical; populated from PackageVersion.NativeSdkVersion or legacy PlayCoreVersion/PgsV2SdkVersion

        [NonSerialized, Obsolete("Use NativeSdkVersion. Removed in editor.core 2.0.0.", error: false)]
        public string PlayCoreVersion;

        /// <summary>
        /// Git URL with the latest tag fragment appended, suitable for UPM install.
        /// Falls back to bare repo URL when no tag is known.
        /// </summary>
        public string GitInstallUrl =>
            string.IsNullOrEmpty(LatestTag) ? GitRepoUrl : $"{GitRepoUrl}#{LatestTag}";

        /// <summary>
        /// True when the installed version differs from the latest remote tag.
        /// </summary>
        public bool HasUpdate =>
            IsInstalled
            && !string.IsNullOrEmpty(LatestTag)
            && !string.IsNullOrEmpty(InstalledVersion)
            && LatestTag.TrimStart('v') != InstalledVersion;
    }
}
