using System.Security.Cryptography;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Persistence.Packaging;

namespace CraftMCP.Persistence.Assets;

public sealed class CraftAssetImporter
{
    public PackagedAssetContent Import(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be blank.", nameof(filePath));
        }

        var fullPath = Path.GetFullPath(filePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Asset file '{fullPath}' does not exist.", fullPath);
        }

        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        var mediaType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => throw new NotSupportedException($"Unsupported asset extension '{extension}'."),
        };

        var bytes = File.ReadAllBytes(fullPath);
        var contentHash = ToLowerHex(SHA256.HashData(bytes));
        var assetId = AssetId.From($"asset_{contentHash}");
        var manifest = new AssetManifestEntry(assetId, Path.GetFileName(fullPath), mediaType, contentHash);
        return new PackagedAssetContent(manifest, CraftPackageConstants.GetAssetEntryPath(manifest), bytes);
    }

    private static string ToLowerHex(byte[] hash) => Convert.ToHexString(hash).ToLowerInvariant();
}
