using CraftMCP.Domain.Models;

namespace CraftMCP.Persistence.Contracts;

public sealed record PackagedAssetContent
{
    public PackagedAssetContent(AssetManifestEntry manifest, string packagePath, byte[] bytes)
    {
        if (string.IsNullOrWhiteSpace(packagePath))
        {
            throw new ArgumentException("Package path cannot be blank.", nameof(packagePath));
        }

        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        PackagePath = packagePath;
        Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    public AssetManifestEntry Manifest { get; init; }

    public string PackagePath { get; init; }

    public byte[] Bytes { get; init; }
}
