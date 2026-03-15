using CraftMCP.Persistence.Packaging;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Persistence;

public sealed class CraftPackageRoundTripTests
{
    [Theory]
    [InlineData("social-graphic.v1.craft", "social-graphic")]
    [InlineData("ui-mockup.v1.craft", "ui-mockup")]
    [InlineData("slide.v1.craft", "slide")]
    public void CommittedCraftFixtures_LoadAndMatchExpectedDocuments(string fixtureFileName, string fixtureName)
    {
        var expectedPackage = fixtureName switch
        {
            "social-graphic" => PersistenceFixtureFactory.CreateSocialGraphicPackage(),
            "ui-mockup" => PersistenceFixtureFactory.CreateUiMockupPackage(),
            "slide" => PersistenceFixtureFactory.CreateSlidePackage(),
            _ => throw new InvalidOperationException($"Unknown fixture '{fixtureName}'."),
        };

        var reader = new CraftPackageReader();
        var result = reader.Read(FixtureFile.CraftPath(fixtureFileName));

        Assert.Equal(
            PersistenceFixtureFactory.SerializeCanonicalDocument(expectedPackage.Document),
            PersistenceFixtureFactory.SerializeCanonicalDocument(result.Package.Document));
        Assert.Equal(
            PersistenceFixtureFactory.ExportJson(expectedPackage),
            PersistenceFixtureFactory.ExportJson(result.Package));
    }

    [Theory]
    [MemberData(nameof(PersistenceFixtureFactory.RoundTripCases), MemberType = typeof(PersistenceFixtureFactory))]
    public void Package_SaveLoadExportRoundTripPreservesDocumentAndExport(string _, object packageObject)
    {
        var packageDocument = Assert.IsType<CraftMCP.Persistence.Contracts.CraftPackageDocument>(packageObject);
        var writer = new CraftPackageWriter();
        var reader = new CraftPackageReader();
        using var stream = new MemoryStream();

        var expectedExport = PersistenceFixtureFactory.ExportJson(packageDocument);
        writer.Write(stream, packageDocument);
        stream.Position = 0;
        var loaded = reader.Read(stream);
        var actualExport = PersistenceFixtureFactory.ExportJson(loaded.Package);

        Assert.Equal(
            PersistenceFixtureFactory.SerializeCanonicalDocument(packageDocument.Document),
            PersistenceFixtureFactory.SerializeCanonicalDocument(loaded.Package.Document));
        Assert.Equal(expectedExport, actualExport);
    }
}
