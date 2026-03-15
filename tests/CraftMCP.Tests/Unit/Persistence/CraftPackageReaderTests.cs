using System.IO.Compression;
using System.Text;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Persistence.Packaging;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Persistence;

public sealed class CraftPackageReaderTests
{
    [Fact]
    public void Read_RejectsPackagesMissingMetadata()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(CraftPackageConstants.DocumentEntryName);
            using var writer = new StreamWriter(entry.Open(), Encoding.UTF8, leaveOpen: false);
            writer.Write("{}");
        }

        stream.Position = 0;
        var reader = new CraftPackageReader();

        var exception = Assert.Throws<CraftPackageException>(() => reader.Read(stream));

        Assert.Equal("package_entry_missing", exception.Code);
    }

    [Fact]
    public void Read_RejectsPackagesWithSchemaMismatchBetweenMetaAndDocument()
    {
        var packageDocument = PersistenceFixtureFactory.CreateSlidePackage();
        var writer = new CraftPackageWriter();
        using var stream = new MemoryStream();
        writer.Write(stream, packageDocument);

        RewriteEntry(stream, CraftPackageConstants.MetadataEntryName, """
{"packageVersion":"v1","documentSchemaVersion":"v999","documentId":"doc_slide_export","assetCount":0}
""");

        stream.Position = 0;
        var reader = new CraftPackageReader();

        var exception = Assert.Throws<CraftPackageException>(() => reader.Read(stream));

        Assert.Equal("schema_version_mismatch", exception.Code);
    }

    [Fact]
    public void Read_WarnsWhenAssetPayloadIsMissingButHydratesDocument()
    {
        var packageDocument = PersistenceFixtureFactory.CreateSocialGraphicPackage();
        var writer = new CraftPackageWriter();
        using var stream = new MemoryStream();
        writer.Write(stream, packageDocument);
        RewriteWithoutAsset(stream, packageDocument.Assets.Values.Single().PackagePath);

        stream.Position = 0;
        var reader = new CraftPackageReader();

        var result = reader.Read(stream);

        Assert.Equal(
            PersistenceFixtureFactory.SerializeCanonicalDocument(packageDocument.Document),
            PersistenceFixtureFactory.SerializeCanonicalDocument(result.Package.Document));
        Assert.Empty(result.Package.Assets);
        var warning = Assert.Single(result.Warnings);
        Assert.Equal("asset_payload_missing", warning.Code);
        Assert.Equal(packageDocument.Document.Assets.Keys.Single(), warning.AssetId);
    }

    private static void RewriteWithoutAsset(MemoryStream sourceStream, string assetEntryName)
    {
        sourceStream.Position = 0;
        using var sourceArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read, leaveOpen: true);
        using var targetStream = new MemoryStream();
        using (var targetArchive = new ZipArchive(targetStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in sourceArchive.Entries.Where(entry => !string.Equals(entry.FullName, assetEntryName, StringComparison.Ordinal)))
            {
                var targetEntry = targetArchive.CreateEntry(entry.FullName);
                using var input = entry.Open();
                using var output = targetEntry.Open();
                input.CopyTo(output);
            }
        }

        sourceStream.SetLength(0);
        targetStream.Position = 0;
        targetStream.CopyTo(sourceStream);
    }

    private static void RewriteEntry(MemoryStream sourceStream, string entryName, string content)
    {
        sourceStream.Position = 0;
        using var sourceArchive = new ZipArchive(sourceStream, ZipArchiveMode.Read, leaveOpen: true);
        using var targetStream = new MemoryStream();
        using (var targetArchive = new ZipArchive(targetStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in sourceArchive.Entries)
            {
                var targetEntry = targetArchive.CreateEntry(entry.FullName);
                using var output = new StreamWriter(targetEntry.Open(), Encoding.UTF8, leaveOpen: false);
                if (string.Equals(entry.FullName, entryName, StringComparison.Ordinal))
                {
                    output.Write(content.Replace("\r\n", "\n", StringComparison.Ordinal));
                }
                else
                {
                    using var input = new StreamReader(entry.Open(), Encoding.UTF8, leaveOpen: false);
                    output.Write(input.ReadToEnd());
                }
            }
        }

        sourceStream.SetLength(0);
        targetStream.Position = 0;
        targetStream.CopyTo(sourceStream);
    }
}
