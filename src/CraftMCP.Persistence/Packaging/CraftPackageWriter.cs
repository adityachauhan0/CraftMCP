using System.IO.Compression;
using System.Text.Json;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.Validation;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Persistence.IO;

namespace CraftMCP.Persistence.Packaging;

public sealed class CraftPackageWriter
{
    private readonly AtomicPackageFileWriter _atomicFileWriter;

    public CraftPackageWriter()
        : this(new AtomicPackageFileWriter())
    {
    }

    internal CraftPackageWriter(AtomicPackageFileWriter atomicFileWriter)
    {
        _atomicFileWriter = atomicFileWriter;
    }

    public byte[] WriteToBytes(CraftPackageDocument package)
    {
        using var stream = new MemoryStream();
        Write(stream, package);
        return stream.ToArray();
    }

    public void Save(string targetPath, CraftPackageDocument package)
    {
        ArgumentNullException.ThrowIfNull(package);
        _atomicFileWriter.Write(targetPath, stream => Write(stream, package));
    }

    public void Write(Stream output, CraftPackageDocument package)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(package);

        ValidateDocument(package);

        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        WriteJsonEntry(archive, CraftPackageConstants.DocumentEntryName, package.Document);
        WriteJsonEntry(archive, CraftPackageConstants.MetadataEntryName, package.Metadata);

        var writtenAssets = new Dictionary<string, byte[]>(StringComparer.Ordinal);

        foreach (var asset in package.Document.Assets.Values.OrderBy(asset => asset.Id.Value, StringComparer.Ordinal))
        {
            if (!package.Assets.TryGetValue(asset.Id, out var packagedAsset))
            {
                throw new CraftPackageException(
                    "asset_payload_missing",
                    $"Package payload bytes are missing for asset '{asset.Id}'.");
            }

            EnsureManifestMatches(packagedAsset, asset);

            if (writtenAssets.TryGetValue(packagedAsset.PackagePath, out var existingBytes))
            {
                if (!existingBytes.SequenceEqual(packagedAsset.Bytes))
                {
                    throw new CraftPackageException(
                        "asset_payload_conflict",
                        $"Package asset path '{packagedAsset.PackagePath}' maps to different payload bytes.");
                }

                continue;
            }

            WriteBinaryEntry(archive, packagedAsset.PackagePath, packagedAsset.Bytes);
            writtenAssets.Add(packagedAsset.PackagePath, packagedAsset.Bytes);
        }
    }

    private static void ValidateDocument(CraftPackageDocument package)
    {
        var validation = DocumentValidator.Validate(package.Document);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(error => error.Message));
            throw new CraftPackageException("document_invalid", $"DocumentState must be valid before packaging. {errors}");
        }
    }

    private static void EnsureManifestMatches(PackagedAssetContent packagedAsset, CraftMCP.Domain.Models.AssetManifestEntry manifest)
    {
        var expectedPath = CraftPackageConstants.GetAssetEntryPath(manifest);
        if (!string.Equals(packagedAsset.PackagePath, expectedPath, StringComparison.Ordinal))
        {
            throw new CraftPackageException(
                "asset_package_path_mismatch",
                $"Asset '{manifest.Id}' package path '{packagedAsset.PackagePath}' does not match expected path '{expectedPath}'.");
        }

        if (packagedAsset.Manifest != manifest)
        {
            throw new CraftPackageException(
                "asset_manifest_mismatch",
                $"Asset '{manifest.Id}' packaged metadata does not match the document manifest.");
        }
    }

    private static void WriteJsonEntry<T>(ZipArchive archive, string entryName, T value)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        JsonSerializer.Serialize(entryStream, value, CraftJsonSerializerOptions.Default);
    }

    private static void WriteBinaryEntry(ZipArchive archive, string entryName, byte[] bytes)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        entryStream.Write(bytes, 0, bytes.Length);
    }
}
