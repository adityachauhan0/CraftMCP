namespace CraftMCP.Domain.Ids;

public static class StableIdGenerator
{
    public static DocumentId CreateDocumentId() => DocumentId.From(Create("doc"));

    public static NodeId CreateNodeId() => NodeId.From(Create("node"));

    public static AssetId CreateAssetId() => AssetId.From(Create("asset"));

    private static string Create(string prefix) => $"{prefix}_{Guid.NewGuid():N}";
}
