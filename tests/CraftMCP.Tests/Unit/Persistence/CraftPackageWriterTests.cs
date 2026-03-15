using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Serialization;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Persistence.Packaging;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Persistence;

public sealed class CraftPackageWriterTests
{
    [Fact]
    public void Write_CreatesRequiredEntriesWithCanonicalDocumentPayload()
    {
        var packageDocument = PersistenceFixtureFactory.CreateSocialGraphicPackage();
        var writer = new CraftPackageWriter();
        using var stream = new MemoryStream();

        writer.Write(stream, packageDocument);

        stream.Position = 0;
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        var entryNames = archive.Entries.Select(entry => entry.FullName).OrderBy(name => name, StringComparer.Ordinal).ToArray();
        Assert.Contains(CraftPackageConstants.DocumentEntryName, entryNames);
        Assert.Contains(CraftPackageConstants.MetadataEntryName, entryNames);
        Assert.Contains(packageDocument.Assets.Values.Single().PackagePath, entryNames);
        Assert.DoesNotContain(CraftPackageConstants.PreviewEntryName, entryNames);

        using var documentReader = new StreamReader(archive.GetEntry(CraftPackageConstants.DocumentEntryName)!.Open(), Encoding.UTF8, leaveOpen: false);
        var documentJson = documentReader.ReadToEnd();
        var restored = JsonSerializer.Deserialize<DocumentState>(documentJson, CraftJsonSerializerOptions.Default);

        Assert.NotNull(restored);
        Assert.Equal(
            PersistenceFixtureFactory.SerializeCanonicalDocument(packageDocument.Document),
            PersistenceFixtureFactory.SerializeCanonicalDocument(restored!));
        Assert.DoesNotContain(Path.GetTempPath(), documentJson, StringComparison.OrdinalIgnoreCase);

        using var metadataReader = new StreamReader(archive.GetEntry(CraftPackageConstants.MetadataEntryName)!.Open(), Encoding.UTF8, leaveOpen: false);
        var metadataJson = metadataReader.ReadToEnd();
        var metadata = JsonSerializer.Deserialize<CraftPackageMetadata>(metadataJson, CraftJsonSerializerOptions.Default);

        Assert.NotNull(metadata);
        Assert.Equal(CraftPackageConstants.PackageVersion, metadata.PackageVersion);
        Assert.Equal(packageDocument.Document.SchemaVersion, metadata.DocumentSchemaVersion);
        Assert.Equal(packageDocument.Document.Id, metadata.DocumentId);
        Assert.Equal(packageDocument.Document.Assets.Count, metadata.AssetCount);
    }

    [Fact]
    public void Write_FailsWhenManifestAssetPayloadIsMissing()
    {
        var packageDocument = PersistenceFixtureFactory.CreateSocialGraphicPackage();
        var incomplete = new CraftPackageDocument(
            packageDocument.Document,
            new Dictionary<CraftMCP.Domain.Ids.AssetId, PackagedAssetContent>());
        var writer = new CraftPackageWriter();
        using var stream = new MemoryStream();

        var exception = Assert.Throws<CraftPackageException>(() => writer.Write(stream, incomplete));

        Assert.Equal("asset_payload_missing", exception.Code);
    }
}
