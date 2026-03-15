using CraftMCP.Domain.Ids;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Rendering.Scene;

public sealed record DocumentRenderPlan(
    DocumentId DocumentId,
    double Width,
    double Height,
    ColorValue Background,
    SafeAreaInsets? SafeArea,
    IReadOnlyList<DocumentRenderNode> Nodes);
