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

        [NonSerialized] public string LatestTag;
        [NonSerialized] public string InstalledVersion;
        [NonSerialized] public bool IsInstalled;

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
