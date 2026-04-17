using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.Editor.Core.EditorTests
{
    /// <summary>
    /// ADR-010 security controls drift guard for
    /// <see cref="FirebaseUpdater"/>. Covers the four primitives that any
    /// change to the updater must continue to honour: URL allowlist, zip
    /// path-traversal guard, SHA256 sums parsing, and GitHub asset JSON
    /// parsing. The integration flow itself is not exercised (network +
    /// interactive import dialog are out of scope for unit tests).
    /// </summary>
    [TestFixture]
    public class FirebaseUpdaterSecurityTest
    {
        // ─── URL allowlist ────────────────────────────────────────────────

        [TestCase("https://github.com/firebase/firebase-unity-sdk/releases/latest", true)]
        [TestCase("https://github.com/firebase/firebase-unity-sdk/releases/download/12.7.0/firebase_unity_sdk_12.7.0.zip", true)]
        [TestCase("https://api.github.com/repos/firebase/firebase-unity-sdk/releases/latest", true)]
        [TestCase("https://objects.githubusercontent.com/github-production-release-asset-2e65be/...", true)]
        [TestCase("http://github.com/firebase/firebase-unity-sdk/releases/latest", false)]        // HTTP
        [TestCase("https://github.com/attacker/firebase-unity-sdk", false)]                      // Wrong org
        [TestCase("https://api.github.com/repos/attacker/firebase-unity-sdk/releases/latest", false)] // Wrong org on API
        [TestCase("https://evil.com/firebase_unity_sdk_12.7.0.zip", false)]                      // Arbitrary host
        [TestCase("javascript:alert(1)", false)]                                                  // Protocol injection
        [TestCase("", false)]
        [TestCase(null, false)]
        public void IsAllowedUrl_AcceptsFirebaseOrgRejectsOthers(string url, bool expected)
        {
            Assert.AreEqual(expected, FirebaseUpdater.IsAllowedUrl(url));
        }

        // ─── Zip path-traversal guard ─────────────────────────────────────

        [Test]
        public void FindUnsafeZipEntry_SafeArchive_ReturnsNull()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bizsim_zip_safe_" + System.Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                string zip = Path.Combine(dir, "safe.zip");
                using (var fs = File.Create(zip))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    archive.CreateEntry("FirebaseAnalytics.unitypackage");
                    archive.CreateEntry("dotnet4/FirebaseCore.unitypackage");
                    archive.CreateEntry("readme.md");
                }

                string extractDir = Path.Combine(dir, "extract");
                Directory.CreateDirectory(extractDir);
                Assert.IsNull(FirebaseUpdater.FindUnsafeZipEntry(zip, extractDir),
                    "Well-formed archive with relative entries must not trip the guard.");
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Test]
        public void FindUnsafeZipEntry_ParentTraversal_Returned()
        {
            string dir = Path.Combine(Path.GetTempPath(), "bizsim_zip_evil_" + System.Guid.NewGuid());
            Directory.CreateDirectory(dir);
            try
            {
                string zip = Path.Combine(dir, "evil.zip");
                using (var fs = File.Create(zip))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    archive.CreateEntry("safe.txt");
                    archive.CreateEntry("../escape.txt");
                }

                string extractDir = Path.Combine(dir, "extract");
                Directory.CreateDirectory(extractDir);
                var offender = FirebaseUpdater.FindUnsafeZipEntry(zip, extractDir);
                Assert.IsNotNull(offender, "Archive with '../' entry must be flagged unsafe.");
                StringAssert.Contains("escape", offender);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        // ─── SHA256 sums parser ───────────────────────────────────────────

        [Test]
        public void LookupExpectedSha256_GnuTwoSpaceFormat_Works()
        {
            const string sums =
                "abc123  firebase_unity_sdk_12.7.0.zip\n" +
                "deadbeef  other.zip\n";

            Assert.AreEqual("abc123",
                FirebaseUpdater.LookupExpectedSha256(sums, "firebase_unity_sdk_12.7.0.zip"),
                "Standard GNU two-space format must be parsed.");
        }

        [Test]
        public void LookupExpectedSha256_SingleSpaceFormat_Works()
        {
            const string sums = "cafebabe firebase_unity_sdk_12.7.0.zip\n";

            Assert.AreEqual("cafebabe",
                FirebaseUpdater.LookupExpectedSha256(sums, "firebase_unity_sdk_12.7.0.zip"),
                "Single-space variants must be parsed (some tools emit them).");
        }

        [Test]
        public void LookupExpectedSha256_MissingFile_ReturnsNull()
        {
            const string sums = "abc123  something_else.zip\n";
            Assert.IsNull(FirebaseUpdater.LookupExpectedSha256(sums, "firebase_unity_sdk_12.7.0.zip"));
        }

        [Test]
        public void LookupExpectedSha256_CommentsAndBlankLines_Skipped()
        {
            const string sums =
                "# header comment\n" +
                "\n" +
                "abc123  firebase_unity_sdk_12.7.0.zip\n";
            Assert.AreEqual("abc123",
                FirebaseUpdater.LookupExpectedSha256(sums, "firebase_unity_sdk_12.7.0.zip"));
        }

        // ─── GitHub asset JSON parser ─────────────────────────────────────

        [Test]
        public void ParseReleaseAssets_MinimalGithubResponse_ExtractsNameAndUrl()
        {
            const string json = @"{
                ""assets"": [
                    {""name"": ""firebase_unity_sdk_12.7.0.zip"", ""label"": null, ""content_type"": ""application/zip"", ""browser_download_url"": ""https://github.com/firebase/firebase-unity-sdk/releases/download/12.7.0/firebase_unity_sdk_12.7.0.zip""},
                    {""name"": ""sha256sums.txt"", ""label"": null, ""content_type"": ""text/plain"", ""browser_download_url"": ""https://github.com/firebase/firebase-unity-sdk/releases/download/12.7.0/sha256sums.txt""}
                ]
            }";

            var assets = FirebaseUpdater.ParseReleaseAssets(json);
            Assert.AreEqual(2, assets.Count);
            Assert.AreEqual("firebase_unity_sdk_12.7.0.zip", assets[0].Name);
            StringAssert.StartsWith("https://github.com/firebase/firebase-unity-sdk/releases/", assets[0].Url);
            Assert.AreEqual("sha256sums.txt", assets[1].Name);
        }
    }
}
