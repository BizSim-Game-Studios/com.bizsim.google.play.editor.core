using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.Editor.Core.EditorTests
{
    /// <summary>
    /// Plan H-2 drift guard. Verifies that
    /// <c>RemoteVersionChecker.PropagateFirebaseTagToEntries</c> populates every
    /// Firebase entry's <c>LatestTag</c> from the shared
    /// <c>LatestFirebaseTag</c>, enabling per-module <c>HasUpdate</c> dots in the
    /// dashboard without per-module network calls.
    /// </summary>
    [TestFixture]
    public class FirebaseTagPropagationTest
    {
        private PropertyInfo _latestTagProperty;

        [SetUp]
        public void SetUp()
        {
            _latestTagProperty = typeof(RemoteVersionChecker)
                .GetProperty(nameof(RemoteVersionChecker.LatestFirebaseTag),
                    BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(_latestTagProperty, "LatestFirebaseTag property not found.");
        }

        private void SetLatestFirebaseTag(string tag)
        {
            _latestTagProperty.GetSetMethod(nonPublic: true).Invoke(null, new object[] { tag });
        }

        private static PackageRegistryData MakeRegistryWithFirebaseEntries(params string[] packageIds)
        {
            var reg = new PackageRegistryData
            {
                FirebasePackages = new List<PackageRegistryEntry>(),
                BizSimPackages = new List<PackageRegistryEntry>(),
                GooglePlayPackages = new List<PackageRegistryEntry>(),
            };
            foreach (var id in packageIds)
            {
                reg.FirebasePackages.Add(new PackageRegistryEntry
                {
                    PackageId = id,
                    AssemblyName = "Firebase." + id.Split('.').Last(),
                    Category = PackageCategory.Firebase,
                });
            }
            return reg;
        }

        [Test]
        public void Propagate_AssignsLatestTagToEveryFirebaseEntry()
        {
            SetLatestFirebaseTag("v12.7.0");
            var registry = MakeRegistryWithFirebaseEntries(
                "com.google.firebase.analytics",
                "com.google.firebase.auth",
                "com.google.firebase.firestore");

            RemoteVersionChecker.PropagateFirebaseTagToEntries(registry);

            foreach (var entry in registry.FirebasePackages)
                Assert.AreEqual("v12.7.0", entry.LatestTag,
                    $"Entry {entry.PackageId} did not receive LatestTag.");
        }

        [Test]
        public void Propagate_NullRegistry_DoesNotThrow()
        {
            SetLatestFirebaseTag("v12.7.0");
            Assert.DoesNotThrow(() => RemoteVersionChecker.PropagateFirebaseTagToEntries(null));
        }

        [Test]
        public void Propagate_EmptyLatestTag_LeavesEntriesUntouched()
        {
            SetLatestFirebaseTag(null);
            var registry = MakeRegistryWithFirebaseEntries("com.google.firebase.analytics");
            registry.FirebasePackages[0].LatestTag = "pre-existing";

            RemoteVersionChecker.PropagateFirebaseTagToEntries(registry);

            Assert.AreEqual("pre-existing", registry.FirebasePackages[0].LatestTag,
                "Empty LatestFirebaseTag should be a no-op — pre-existing value must be preserved.");
        }

        [Test]
        public void HasUpdate_TrueWhenInstalledVersionDiffersFromPropagatedTag()
        {
            SetLatestFirebaseTag("v12.7.0");
            var registry = MakeRegistryWithFirebaseEntries("com.google.firebase.analytics");
            var entry = registry.FirebasePackages[0];
            entry.IsInstalled = true;
            entry.InstalledVersion = "12.6.0";

            RemoteVersionChecker.PropagateFirebaseTagToEntries(registry);

            Assert.IsTrue(entry.HasUpdate,
                "HasUpdate should be true when InstalledVersion (12.6.0) differs from propagated LatestTag (v12.7.0).");
        }

        [Test]
        public void HasUpdate_FalseWhenInstalledVersionMatchesPropagatedTag()
        {
            SetLatestFirebaseTag("v12.7.0");
            var registry = MakeRegistryWithFirebaseEntries("com.google.firebase.analytics");
            var entry = registry.FirebasePackages[0];
            entry.IsInstalled = true;
            entry.InstalledVersion = "12.7.0";

            RemoteVersionChecker.PropagateFirebaseTagToEntries(registry);

            Assert.IsFalse(entry.HasUpdate,
                "HasUpdate should be false when InstalledVersion (12.7.0) matches propagated LatestTag (v12.7.0).");
        }
    }
}
