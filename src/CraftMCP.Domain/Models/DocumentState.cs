using System.Text.Json.Serialization;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Domain.Models;

public sealed record DocumentState
{
    [JsonConstructor]
    public DocumentState(
        DocumentId id,
        SchemaVersion schemaVersion,
        string name,
        CanvasModel canvas,
        IReadOnlyDictionary<NodeId, NodeBase> nodes,
        IReadOnlyList<NodeId> rootNodeIds,
        IReadOnlyDictionary<AssetId, AssetManifestEntry> assets)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Document name cannot be blank.", nameof(name));
        }

        Id = id;
        SchemaVersion = schemaVersion;
        Name = name;
        Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        Nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));
        RootNodeIds = rootNodeIds ?? throw new ArgumentNullException(nameof(rootNodeIds));
        Assets = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public DocumentId Id { get; init; }

    public SchemaVersion SchemaVersion { get; init; }

    public string Name { get; init; }

    public CanvasModel Canvas { get; init; }

    public IReadOnlyDictionary<NodeId, NodeBase> Nodes { get; init; }

    public IReadOnlyList<NodeId> RootNodeIds { get; init; }

    public IReadOnlyDictionary<AssetId, AssetManifestEntry> Assets { get; init; }
}
