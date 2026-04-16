using NUnit.Framework;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.Editor.Core.EditorTests
{
    [TestFixture]
    public class ScopedRegistryConfiguratorTests
    {
        const string ManifestNoRegistries = @"{
  ""dependencies"": {
    ""com.unity.textmeshpro"": ""3.0.9""
  }
}";

        const string ManifestEmptyRegistries = @"{
  ""dependencies"": {
    ""com.unity.textmeshpro"": ""3.0.9""
  },
  ""scopedRegistries"": []
}";

        const string ManifestWithOpenUpm = @"{
  ""dependencies"": {
    ""com.unity.textmeshpro"": ""3.0.9""
  },
  ""scopedRegistries"": [
    {
      ""name"": ""package.openupm.com"",
      ""url"": ""https://package.openupm.com"",
      ""scopes"": [
        ""com.google.external-dependency-manager"",
        ""com.google.firebase""
      ]
    }
  ]
}";

        [Test]
        public void EnsureOpenUpmRegistry_AddsRegistryWhenMissing()
        {
            string result = ScopedRegistryConfigurator.EnsureOpenUpmRegistry(ManifestNoRegistries);

            Assert.That(result, Does.Contain("package.openupm.com"));
            Assert.That(result, Does.Contain("https://package.openupm.com"));
            Assert.That(result, Does.Contain("com.google.external-dependency-manager"));
            Assert.That(result, Does.Contain("com.google.firebase"));
        }

        [Test]
        public void EnsureOpenUpmRegistry_AddsToEmptyArray()
        {
            string result = ScopedRegistryConfigurator.EnsureOpenUpmRegistry(ManifestEmptyRegistries);

            Assert.That(result, Does.Contain("package.openupm.com"));
            Assert.That(result, Does.Contain("com.google.external-dependency-manager"));
            Assert.That(result, Does.Contain("com.google.firebase"));
        }

        [Test]
        public void EnsureOpenUpmRegistry_IdempotentWhenAlreadyPresent()
        {
            string result = ScopedRegistryConfigurator.EnsureOpenUpmRegistry(ManifestWithOpenUpm);

            Assert.AreEqual(ManifestWithOpenUpm, result);
        }

        [Test]
        public void EnsureOpenUpmRegistry_NullInput_ReturnsNull()
        {
            Assert.IsNull(ScopedRegistryConfigurator.EnsureOpenUpmRegistry(null));
        }

        [Test]
        public void EnsureOpenUpmRegistry_EmptyInput_ReturnsEmpty()
        {
            Assert.AreEqual("", ScopedRegistryConfigurator.EnsureOpenUpmRegistry(""));
        }

        [Test]
        public void EnsureOpenUpmRegistry_PreservesExistingDependencies()
        {
            string result = ScopedRegistryConfigurator.EnsureOpenUpmRegistry(ManifestNoRegistries);

            Assert.That(result, Does.Contain("com.unity.textmeshpro"));
            Assert.That(result, Does.Contain("3.0.9"));
        }

        [Test]
        public void EnsureOpenUpmRegistry_AppendsToExistingRegistries()
        {
            string input = @"{
  ""dependencies"": {},
  ""scopedRegistries"": [
    {
      ""name"": ""other-registry"",
      ""url"": ""https://other.example.com"",
      ""scopes"": [""com.other""]
    }
  ]
}";
            string result = ScopedRegistryConfigurator.EnsureOpenUpmRegistry(input);

            Assert.That(result, Does.Contain("other-registry"));
            Assert.That(result, Does.Contain("package.openupm.com"));
        }
    }
}
