using CraftMCP.Domain.Models;

namespace CraftMCP.Persistence.Packaging;

public static class CraftPackageConstants
{
    public const string PackageVersion = "v1";
    public const string DocumentEntryName = "document.json";
    public const string MetadataEntryName = "meta.json";
    public const string PreviewEntryName = "preview.png";
    public const string AssetsDirectoryName = "assets/";

    public static string GetAssetEntryPath(AssetManifestEntry asset)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var extension = Path.GetExtension(asset.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException($"Asset '{asset.Id}' file name '{asset.FileName}' must include an extension.");
        }

        return $"{AssetsDirectoryName}{asset.ContentHash}{extension.ToLowerInvariant()}";
    }
}
