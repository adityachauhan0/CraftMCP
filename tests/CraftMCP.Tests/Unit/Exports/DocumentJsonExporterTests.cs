using CraftMCP.Domain.Exports;
using CraftMCP.Domain.Models;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Exports;

public sealed class DocumentJsonExporterTests
{
    [Theory]
    [MemberData(nameof(DocumentExportFixtureFactory.FixtureCases), MemberType = typeof(DocumentExportFixtureFactory))]
    public void Serialize_MatchesExpectedFixture(string fixtureFileName, DocumentState document)
    {
        var json = DocumentJsonExporter.Serialize(document);
        var expected = FixtureFile.ReadJson(fixtureFileName);

        Assert.Equal(expected, json);
    }

    [Theory]
    [MemberData(nameof(DocumentExportFixtureFactory.FixtureCases), MemberType = typeof(DocumentExportFixtureFactory))]
    public void Serialize_IsByteStableAcrossRepeatedExports(string fixtureFileName, DocumentState document)
    {
        var first = DocumentJsonExporter.Serialize(document);
        var second = DocumentJsonExporter.Serialize(document);
        var third = DocumentJsonExporter.Serialize(document);

        Assert.Equal(first, second);
        Assert.Equal(second, third);
        Assert.Equal(FixtureFile.ReadJson(fixtureFileName), third);
    }

    [Theory]
    [MemberData(nameof(DocumentExportFixtureFactory.ShuffledFixtureCases), MemberType = typeof(DocumentExportFixtureFactory))]
    public void Serialize_IgnoresInsertionOrderForNodesAndAssets(DocumentState orderedDocument, DocumentState shuffledDocument)
    {
        var orderedJson = DocumentJsonExporter.Serialize(orderedDocument);
        var shuffledJson = DocumentJsonExporter.Serialize(shuffledDocument);

        Assert.Equal(orderedJson, shuffledJson);
    }
}
