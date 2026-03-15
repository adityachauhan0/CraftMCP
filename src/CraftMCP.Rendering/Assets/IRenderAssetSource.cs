using CraftMCP.Domain.Ids;

namespace CraftMCP.Rendering.Assets;

public interface IRenderAssetSource
{
    bool TryGetBytes(AssetId assetId, out byte[] bytes);
}
