using CraftMCP.Domain.Ids;

namespace CraftMCP.Rendering.Assets;

public sealed class InMemoryRenderAssetSource : IRenderAssetSource
{
    private readonly IReadOnlyDictionary<AssetId, byte[]> _assets;

    public static InMemoryRenderAssetSource Empty { get; } =
        new(new Dictionary<AssetId, byte[]>());

    public InMemoryRenderAssetSource(IReadOnlyDictionary<AssetId, byte[]> assets)
    {
        ArgumentNullException.ThrowIfNull(assets);

        _assets = assets.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ToArray());
    }

    public bool TryGetBytes(AssetId assetId, out byte[] bytes)
    {
        if (_assets.TryGetValue(assetId, out var assetBytes))
        {
            bytes = assetBytes.ToArray();
            return true;
        }

        bytes = Array.Empty<byte>();
        return false;
    }
}
