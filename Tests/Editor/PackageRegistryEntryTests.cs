using NUnit.Framework;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.Editor.Core.EditorTests
{
    [TestFixture]
    public class PackageRegistryEntryTests
    {
        [Test]
        public void GitInstallUrl_CombinesRepoAndTag()
        {
            var entry = new PackageRegistryEntry
            {
                PackageId = "com.bizsim.google.play.review",
                GitRepoUrl = "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.review.git",
                LatestTag = "v1.3.0"
            };
            Assert.AreEqual(
                "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.review.git#v1.3.0",
                entry.GitInstallUrl);
        }

        [Test]
        public void GitInstallUrl_WithoutTag_ReturnsBaseUrl()
        {
            var entry = new PackageRegistryEntry
            {
                GitRepoUrl = "https://github.com/BizSim-Game-Studios/com.bizsim.google.play.review.git"
            };
            Assert.AreEqual(entry.GitRepoUrl, entry.GitInstallUrl);
        }

        [Test]
        public void HasUpdate_True_WhenVersionsDiffer()
        {
            var entry = new PackageRegistryEntry
            {
                IsInstalled = true,
                InstalledVersion = "1.0.1",
                LatestTag = "v1.3.0"
            };
            Assert.IsTrue(entry.HasUpdate);
        }

        [Test]
        public void HasUpdate_False_WhenSameVersion()
        {
            var entry = new PackageRegistryEntry
            {
                IsInstalled = true,
                InstalledVersion = "1.3.0",
                LatestTag = "v1.3.0"
            };
            Assert.IsFalse(entry.HasUpdate);
        }

        [Test]
        public void HasUpdate_False_WhenNotInstalled()
        {
            var entry = new PackageRegistryEntry
            {
                IsInstalled = false,
                InstalledVersion = "1.0.0",
                LatestTag = "v1.3.0"
            };
            Assert.IsFalse(entry.HasUpdate);
        }

        [Test]
        public void HasUpdate_False_WhenNoLatestTag()
        {
            var entry = new PackageRegistryEntry
            {
                IsInstalled = true,
                InstalledVersion = "1.0.0",
                LatestTag = null
            };
            Assert.IsFalse(entry.HasUpdate);
        }

        [Test]
        public void HasUpdate_False_WhenNoInstalledVersion()
        {
            var entry = new PackageRegistryEntry
            {
                IsInstalled = true,
                InstalledVersion = null,
                LatestTag = "v1.3.0"
            };
            Assert.IsFalse(entry.HasUpdate);
        }
    }
}
