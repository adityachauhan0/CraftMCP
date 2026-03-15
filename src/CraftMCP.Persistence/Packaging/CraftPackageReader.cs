using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Serialization;
using CraftMCP.Domain.Validation;
using CraftMCP.Persistence.Contracts;

namespace CraftMCP.Persistence.Packaging;

public sealed class CraftPackageReader
{
    public CraftPackageLoadResult Read(string packagePath)
    {
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            throw new ArgumentException("Package path cannot be blank.", nameof(packagePath));
        }

        using var stream = File.OpenRead(packagePath);
        return Read(stream);
    }

    public CraftPackageLoadResult Read(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);

        using var archive = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        var metadata = ReadRequiredJsonEntry<CraftPackageMetadata>(archive, CraftPackageConstants.MetadataEntryName);
        ValidateMetadata(metadata);

        var document = ReadRequiredJsonEntry<DocumentState>(archive, CraftPackageConstants.DocumentEntryName);
        ValidateDocument(document, metadata);

        var warnings = new List<CraftPackageWarning>();
        var assets = new Dictionary<CraftMCP.Domain.Ids.AssetId, PackagedAssetContent>();
        var expectedAssetPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var manifest in document.Assets.Values.OrderBy(asset => asset.Id.Value, StringComparer.Ordinal))
        {
            var assetPath = GetExpectedAssetPath(manifest);
            expectedAssetPaths.Add(assetPath);

            var entry = archive.GetEntry(assetPath);
            if (entry is null)
            {
                warnings.Add(new CraftPackageWarning(
                    "asset_payload_missing",
                    $"Package asset payload '{assetPath}' is missing for asset '{manifest.Id}'.",
                    manifest.Id,
                    assetPath));
                continue;
            }

            try
            {
                using var entryStream = entry.Open();
                using var buffer = new MemoryStream();
                entryStream.CopyTo(buffer);
                var bytes = buffer.ToArray();
                var actualHash = ToLowerHex(SHA256.HashData(bytes));
                if (!string.Equals(actualHash, manifest.ContentHash, StringComparison.Ordinal))
                {
                    warnings.Add(new CraftPackageWarning(
                        "asset_content_hash_mismatch",
                        $"Package asset payload '{assetPath}' hash does not match manifest asset '{manifest.Id}'.",
                        manifest.Id,
                        assetPath));
                    continue;
                }

                assets.Add(manifest.Id, new PackagedAssetContent(manifest, assetPath, bytes));
            }
            catch (Exception)
            {
                warnings.Add(new CraftPackageWarning(
                    "asset_payload_corrupt",
                    $"Package asset payload '{assetPath}' could not be read for asset '{manifest.Id}'.",
                    manifest.Id,
                    assetPath));
            }
        }

        foreach (var entry in archive.Entries.Where(entry => entry.FullName.StartsWith(CraftPackageConstants.AssetsDirectoryName, StringComparison.Ordinal)))
        {
            if (!expectedAssetPaths.Contains(entry.FullName))
            {
                warnings.Add(new CraftPackageWarning(
                    "orphan_asset_payload",
                    $"Package asset payload '{entry.FullName}' is not referenced by the document manifest.",
                    PackagePath: entry.FullName));
            }
        }

        var package = new CraftPackageDocument(document, assets);
        return new CraftPackageLoadResult(package, metadata, warnings);
    }

    private static void ValidateMetadata(CraftPackageMetadata metadata)
    {
        if (!string.Equals(metadata.PackageVersion, CraftPackageConstants.PackageVersion, StringComparison.Ordinal))
        {
            throw new CraftPackageException(
                "unsupported_package_version",
                $"Package version '{metadata.PackageVersion}' is not supported.");
        }
    }

    private static void ValidateDocument(DocumentState document, CraftPackageMetadata metadata)
    {
        if (metadata.DocumentSchemaVersion != document.SchemaVersion)
        {
            throw new CraftPackageException(
                "schema_version_mismatch",
                $"Package metadata schema version '{metadata.DocumentSchemaVersion}' does not match document schema version '{document.SchemaVersion}'.");
        }

        if (metadata.DocumentId != document.Id)
        {
            throw new CraftPackageException(
                "document_id_mismatch",
                $"Package metadata document ID '{metadata.DocumentId}' does not match document ID '{document.Id}'.");
        }

        if (metadata.AssetCount != document.Assets.Count)
        {
            throw new CraftPackageException(
                "asset_count_mismatch",
                $"Package metadata asset count '{metadata.AssetCount}' does not match document asset count '{document.Assets.Count}'.");
        }

        var validation = DocumentValidator.Validate(document);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(error => error.Message));
            throw new CraftPackageException("document_invalid", $"Packaged DocumentState is invalid. {errors}");
        }
    }

    private static T ReadRequiredJsonEntry<T>(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        if (entry is null)
        {
            throw new CraftPackageException(
                "package_entry_missing",
                $"Required package entry '{entryName}' is missing.");
        }

        try
        {
            using var entryStream = entry.Open();
            var value = JsonSerializer.Deserialize<T>(entryStream, CraftJsonSerializerOptions.Default);
            return value ?? throw new CraftPackageException(
                "package_json_invalid",
                $"Required package entry '{entryName}' could not be deserialized.");
        }
        catch (JsonException exception)
        {
            throw new CraftPackageException(
                "package_json_invalid",
                $"Required package entry '{entryName}' contains invalid JSON.",
                innerException: exception);
        }
    }

    private static string GetExpectedAssetPath(AssetManifestEntry manifest)
    {
        try
        {
            return CraftPackageConstants.GetAssetEntryPath(manifest);
        }
        catch (Exception exception)
        {
            throw new CraftPackageException(
                "asset_manifest_invalid",
                $"Asset manifest entry '{manifest.Id}' cannot be mapped to a packaged asset path.",
                innerException: exception);
        }
    }

    private static string ToLowerHex(byte[] hash) => Convert.ToHexString(hash).ToLowerInvariant();
}
