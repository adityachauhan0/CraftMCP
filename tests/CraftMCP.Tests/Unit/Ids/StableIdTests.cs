using System.Text.Json;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Serialization;

namespace CraftMCP.Tests.Unit.Ids;

public sealed class StableIdTests
{
    [Fact]
    public void CreateDocumentId_UsesDocumentPrefix()
    {
        var id = StableIdGenerator.CreateDocumentId();

        Assert.StartsWith("doc_", id.Value);
    }

    [Fact]
    public void CreateNodeId_UsesNodePrefix()
    {
        var id = StableIdGenerator.CreateNodeId();

        Assert.StartsWith("node_", id.Value);
    }

    [Fact]
    public void CreateAssetId_UsesAssetPrefix()
    {
        var id = StableIdGenerator.CreateAssetId();

        Assert.StartsWith("asset_", id.Value);
    }

    [Fact]
    public void NodeId_RoundTripsAsJsonString()
    {
        var original = NodeId.From("node_existing");
        var json = JsonSerializer.Serialize(original, CraftJsonSerializerOptions.Default);
        var restored = JsonSerializer.Deserialize<NodeId>(json, CraftJsonSerializerOptions.Default);

        Assert.Equal("\"node_existing\"", json);
        Assert.Equal(original, restored);
    }
}
