namespace BizSim.Google.Play.Editor.Core
{
    /// <summary>
    /// Immutable value type describing a single package install action.
    /// Used by <see cref="PackageInstallQueue"/> to process installs sequentially.
    /// </summary>
    public readonly struct InstallRequest
    {
        /// <summary>Package identifier (e.g. "com.bizsim.google.play.review").</summary>
        public string PackageId { get; }

        /// <summary>
        /// The string passed to UnityEditor.PackageManager.Client.Add.
        /// Either a git URL ("https://...#v1.0.0") or a scoped-registry package id ("com.google.firebase.analytics").
        /// </summary>
        public string InstallIdentifier { get; }

        /// <summary>
        /// True when the package comes from a scoped registry (e.g. OpenUPM)
        /// rather than a direct git URL.
        /// </summary>
        public bool IsScopedRegistry { get; }

        public InstallRequest(string packageId, string installIdentifier, bool isScopedRegistry = false)
        {
            PackageId = packageId;
            InstallIdentifier = installIdentifier;
            IsScopedRegistry = isScopedRegistry;
        }
    }
}
