using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Persistence.Contracts;

public sealed record CraftPackageMetadata
{
    [JsonConstructor]
    public CraftPackageMetadata(string packageVersion, SchemaVersion documentSchemaVersion, DocumentId documentId, int assetCount)
    {
        if (string.IsNullOrWhiteSpace(packageVersion))
        {
            throw new ArgumentException("Package version cannot be blank.", nameof(packageVersion));
        }

        if (assetCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assetCount), "Asset count cannot be negative.");
        }

        PackageVersion = packageVersion;
        DocumentSchemaVersion = documentSchemaVersion;
        DocumentId = documentId;
        AssetCount = assetCount;
    }

    public string PackageVersion { get; init; }

    public SchemaVersion DocumentSchemaVersion { get; init; }

    public DocumentId DocumentId { get; init; }

    public int AssetCount { get; init; }

    public static CraftPackageMetadata From(DocumentState document, string packageVersion) =>
        new(packageVersion, document.SchemaVersion, document.Id, document.Assets.Count);
}
