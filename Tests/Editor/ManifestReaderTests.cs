using System.Collections.Generic;
using NUnit.Framework;
using BizSim.Google.Play.Editor.Core;

namespace BizSim.Google.Play.Editor.Core.EditorTests
{
    [TestFixture]
    public class ManifestReaderTests
    {
        const string SampleManifest = @"
{
  ""dependencies"": {
    ""com.bizsim.google.play.review"": ""https://github.com/BizSim-Game-Studios/com.bizsim.google.play.review.git#v1.0.1"",
    ""com.bizsim.google.play.games"": ""https://github.com/BizSim-Game-Studios/com.bizsim.google.play.games.git#1.0.0"",
    ""com.google.firebase.analytics"": ""12.5.0"",
    ""com.unity.textmeshpro"": ""3.0.9"",
    ""com.bizsim.google.play.editor.core"": ""file:../../unity-packages/com.bizsim.google.play.editor.core""
  },
  ""scopedRegistries"": []
}";

        [Test]
        public void ParseManifest_ExtractsRegistryVersion()
        {
            var result = ManifestReader.ParseManifest(SampleManifest);
            Assert.AreEqual("12.5.0", result["com.google.firebase.analytics"]);
        }

        [Test]
        public void ParseManifest_ExtractsGitUrlWithVPrefix()
        {
            var result = ManifestReader.ParseManifest(SampleManifest);
            Assert.AreEqual("1.0.1", result["com.bizsim.google.play.review"]);
        }

        [Test]
        public void ParseManifest_ExtractsGitUrlWithoutVPrefix()
        {
            var result = ManifestReader.ParseManifest(SampleManifest);
            Assert.AreEqual("1.0.0", result["com.bizsim.google.play.games"]);
        }

        [Test]
        public void ParseManifest_SkipsFileProtocolEntries()
        {
            var result = ManifestReader.ParseManifest(SampleManifest);
            Assert.IsFalse(result.ContainsKey("com.bizsim.google.play.editor.core"));
        }

        [Test]
        public void ParseManifest_EmptyJson_ReturnsEmptyDict()
        {
            var result = ManifestReader.ParseManifest("");
            Assert.IsEmpty(result);
        }

        [Test]
        public void ParseManifest_NoDependenciesKey_ReturnsEmptyDict()
        {
            var result = ManifestReader.ParseManifest(@"{ ""scopedRegistries"": [] }");
            Assert.IsEmpty(result);
        }

        [Test]
        public void ExtractVersion_PlainSemver()
        {
            Assert.AreEqual("1.2.3", ManifestReader.ExtractVersion("1.2.3"));
        }

        [Test]
        public void ExtractVersion_GitUrlWithV()
        {
            Assert.AreEqual("1.0.1",
                ManifestReader.ExtractVersion(
                    "https://github.com/BizSim-Game-Studios/repo.git#v1.0.1"));
        }

        [Test]
        public void ExtractVersion_GitUrlWithoutV()
        {
            Assert.AreEqual("2.0.0",
                ManifestReader.ExtractVersion(
                    "https://github.com/BizSim-Game-Studios/repo.git#2.0.0"));
        }

        [Test]
        public void ExtractVersion_FileProtocol_ReturnsNull()
        {
            Assert.IsNull(ManifestReader.ExtractVersion("file:../../some/path"));
        }

        [Test]
        public void ExtractVersion_PreReleaseTag()
        {
            Assert.AreEqual("1.0.0-beta.1",
                ManifestReader.ExtractVersion(
                    "https://github.com/org/repo.git#v1.0.0-beta.1"));
        }
    }
}
