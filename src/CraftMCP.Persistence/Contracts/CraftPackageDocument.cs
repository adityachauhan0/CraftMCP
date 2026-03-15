using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Persistence.Packaging;

namespace CraftMCP.Persistence.Contracts;

public sealed record CraftPackageDocument
{
    public CraftPackageDocument(DocumentState document, IReadOnlyDictionary<AssetId, PackagedAssetContent> assets)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public DocumentState Document { get; init; }

    public IReadOnlyDictionary<AssetId, PackagedAssetContent> Assets { get; init; }

    public CraftPackageMetadata Metadata => CraftPackageMetadata.From(Document, CraftPackageConstants.PackageVersion);
}
