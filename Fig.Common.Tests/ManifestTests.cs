namespace Fig.Common.Tests
{
    using FluentAssertions;
    using NUnit.Framework;
    using System.Linq;

    public class ManifestTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestManifestChecksum()
        {
            var manifest = new Manifest
            {
                Version = "1.0.0",
                Files = new []
                {
                    new Manifest.File
                    {
                        Checksum = "sha256@000000000000000000000000000000000001",
                        FileName = "Test1.txt"
                    },
                    new Manifest.File
                    {
                        Checksum = "sha256@000000000000000000000000000000000002",
                        FileName = "Test2.txt"
                    },
                },
            };

            manifest.GetChecksum("sha256").Should().Be("sha256@13206fd7e7336a2b77d1b7175bd114df6b68286ccce4e40177b0a056a1da4d9c");

            manifest = new Manifest
            {
                Version = "1.0.1",
                Files = manifest.Files,
            };

            manifest.GetChecksum("sha256").Should().Be("sha256@13206fd7e7336a2b77d1b7175bd114df6b68286ccce4e40177b0a056a1da4d9c", "the checksum shouldn't change if the version number differs");
            
            manifest = new Manifest
            {
                Version = manifest.Version,
                Files = manifest.Files.Reverse(),
            };
            manifest.GetChecksum("sha256").Should().Be("sha256@13206fd7e7336a2b77d1b7175bd114df6b68286ccce4e40177b0a056a1da4d9c", "the checksum should be independent of the file ordering");
        }

        [TestCase("Test1.txt:sha256@000000000000000000000000000000000001,Test2.txt:sha256@000000000000000000000000000000000002", false, Description = "Unique files")]
        [TestCase("Test1.txt:sha256@000000000000000000000000000000000001,Test2.txt:sha256@000000000000000000000000000000000001", false, Description = "Using the same checksum for multiple files")]
        [TestCase("Test1.txt:sha256@000000000000000000000000000000000001,Test1.txt:sha256@000000000000000000000000000000000001", true, Description = "Duplicates of a specific file")]
        [TestCase("Test1.txt:sha256@000000000000000000000000000000000001,Test1.txt:sha256@000000000000000000000000000000000002", true, Description = "Conflicting contents for a specific file")]
        public void TestManifestValidate(string fileSpec, bool throws)
        {
            var manifest = new Manifest
            {
                Version = "1.0.0",
                Files = fileSpec.Split(',').Select(s => s.Split(':')).Select(p => new Manifest.File { FileName=p[0], Checksum = p[1]}),
            };

            if (throws)
                FluentActions.Invoking(() => manifest.Validate()).Should().Throw<Exceptions.FigException>();
            else
                FluentActions.Invoking(() => manifest.Validate()).Should().NotThrow();
        }
    }
}