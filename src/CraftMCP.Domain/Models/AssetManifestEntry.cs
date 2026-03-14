using CraftMCP.Domain.Ids;

namespace CraftMCP.Domain.Models;

public sealed record AssetManifestEntry
{
    public AssetManifestEntry(AssetId id, string fileName, string mediaType, string contentHash)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be blank.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(mediaType))
        {
            throw new ArgumentException("Media type cannot be blank.", nameof(mediaType));
        }

        if (string.IsNullOrWhiteSpace(contentHash))
        {
            throw new ArgumentException("Content hash cannot be blank.", nameof(contentHash));
        }

        Id = id;
        FileName = fileName;
        MediaType = mediaType;
        ContentHash = contentHash;
    }

    public AssetId Id { get; init; }

    public string FileName { get; init; }

    public string MediaType { get; init; }

    public string ContentHash { get; init; }
}
