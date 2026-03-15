using CraftMCP.Persistence.Assets;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Persistence;

public sealed class CraftAssetImporterTests
{
    [Fact]
    public void Import_ComputesStableHashBasedAssetIdentityAndPackagePath()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"craft-import-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(sourcePath, FixtureFile.ReadCraftAsset("social-hero.png"));

        try
        {
            var importer = new CraftAssetImporter();

            var imported = importer.Import(sourcePath);

            Assert.Equal($"asset_{imported.Manifest.ContentHash}", imported.Manifest.Id.Value);
            Assert.Equal("image/png", imported.Manifest.MediaType);
            Assert.Equal($"assets/{imported.Manifest.ContentHash}.png", imported.PackagePath);
            Assert.Equal(Path.GetFileName(sourcePath), imported.Manifest.FileName);
            Assert.NotEmpty(imported.Bytes);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }

    [Fact]
    public void Import_RejectsUnsupportedExtensions()
    {
        var sourcePath = Path.Combine(Path.GetTempPath(), $"craft-import-{Guid.NewGuid():N}.txt");
        File.WriteAllText(sourcePath, "not-an-image");

        try
        {
            var importer = new CraftAssetImporter();

            var exception = Assert.Throws<NotSupportedException>(() => importer.Import(sourcePath));

            Assert.Contains(".txt", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(sourcePath);
        }
    }
}
