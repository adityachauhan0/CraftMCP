using CraftMCP.Domain.Ids;

namespace CraftMCP.Rendering;

public sealed record RenderWarning(
    RenderWarningCode Code,
    string Message,
    NodeId? NodeId = null,
    AssetId? AssetId = null,
    string? RequestedFontFamily = null,
    string? ResolvedFontFamily = null);
