using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.Editor.Core.EditorTests
{
    /// <summary>
    /// K9.2 drift guard (Plan H-1). Asserts that every currently-active Firebase
    /// Unity SDK module is catalogued in <c>PackageRegistry.json</c>. When Firebase
    /// ships a new top-level module (or retires one), this test fails and forces a
    /// registry update in the same PR.
    /// </summary>
    /// <remarks>
    /// The expected-module list is the set of Firebase Unity SDK modules that were
    /// actively maintained as of 2026-04-17 (Firebase Unity SDK v12.x era). Deprecated
    /// modules (DynamicLinks — sunset 2026-08, InAppMessaging — sunset 2025-12) are
    /// intentionally NOT in the list.
    /// </remarks>
    [TestFixture]
    public class FirebaseRegistryCompletenessTest
    {
        // Active Firebase Unity SDK modules as of 2026-04-17. Update this list in
        // lockstep with PackageRegistry.json when Firebase adds or retires a module.
        private static readonly string[] ExpectedPackageIds =
        {
            "com.google.firebase.analytics",
            "com.google.firebase.auth",
            "com.google.firebase.crashlytics",
            "com.google.firebase.remote-config",
            "com.google.firebase.messaging",
            "com.google.firebase.storage",
            "com.google.firebase.firestore",
            "com.google.firebase.functions",
            "com.google.firebase.database",
            "com.google.firebase.app-check",
            "com.google.firebase.installations",
            "com.google.firebase.firebaseai",
            "com.google.firebase.performance",
        };

        [Test]
        public void Registry_ContainsAllActiveFirebaseModules()
        {
            var registry = PackageRegistryData.Load();
            Assert.IsNotNull(registry, "PackageRegistryData.Load() returned null — resource missing or malformed.");
            Assert.IsNotNull(registry.FirebasePackages, "registry.FirebasePackages is null.");

            var registeredIds = registry.FirebasePackages
                .Select(e => e.PackageId)
                .ToHashSet();

            var missing = new List<string>();
            foreach (var expected in ExpectedPackageIds)
            {
                if (!registeredIds.Contains(expected))
                    missing.Add(expected);
            }

            Assert.IsEmpty(missing,
                "Firebase modules missing from PackageRegistry.json: " + string.Join(", ", missing) +
                ". Add them to Editor/Resources/BizSim/PackageRegistry.json FirebasePackages list.");
        }

        [Test]
        public void Registry_FirebaseEntries_HaveUniqueAssemblyNames()
        {
            var registry = PackageRegistryData.Load();
            Assert.IsNotNull(registry);

            var duplicates = registry.FirebasePackages
                .GroupBy(e => e.AssemblyName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.IsEmpty(duplicates,
                "Duplicate Firebase AssemblyName values in PackageRegistry.json: " + string.Join(", ", duplicates));
        }

        [Test]
        public void Registry_FirebaseEntries_UseFirebaseNamespaceAssemblies()
        {
            var registry = PackageRegistryData.Load();
            Assert.IsNotNull(registry);

            var malformed = registry.FirebasePackages
                .Where(e => string.IsNullOrEmpty(e.AssemblyName) || !e.AssemblyName.StartsWith("Firebase."))
                .Select(e => $"{e.PackageId} → '{e.AssemblyName}'")
                .ToList();

            Assert.IsEmpty(malformed,
                "Firebase entries with non-Firebase.* AssemblyName: " + string.Join(", ", malformed) +
                ". Every Firebase Unity SDK module ships as Firebase.<Module>.dll.");
        }

        [Test]
        public void Registry_FirebaseEntries_AreCategoryFirebase()
        {
            var registry = PackageRegistryData.Load();
            Assert.IsNotNull(registry);

            var wrongCategory = registry.FirebasePackages
                .Where(e => e.Category != PackageCategory.Firebase)
                .Select(e => $"{e.PackageId} → Category={e.Category}")
                .ToList();

            Assert.IsEmpty(wrongCategory,
                "Firebase entries with wrong Category: " + string.Join(", ", wrongCategory) +
                ". All FirebasePackages entries must have Category=0 (Firebase).");
        }
    }
}
