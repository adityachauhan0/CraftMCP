using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;
using CraftMCP.Persistence.Contracts;
using CraftMCP.Rendering.Assets;

namespace CraftMCP.Tests.TestSupport;

internal static class RenderingFixtureFactory
{
    public static InMemoryRenderAssetSource CreateAssetSource(CraftPackageDocument package)
    {
        var assets = package.Assets.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.Bytes);

        return new InMemoryRenderAssetSource(assets);
    }

    public static DocumentState CreateFontFallbackDocument()
    {
        var document = DocumentExportFixtureFactory.CreateSlide();
        var titleId = NodeId.From("node_slide_title");
        var title = (TextNode)document.Nodes[titleId];
        var nodes = document.Nodes.ToDictionary(
            entry => entry.Key,
            entry => entry.Key == titleId
                ? (NodeBase)(title with
                {
                    Typography = new TypographyStyle(
                        "CraftMCP Missing Font Family",
                        title.Typography.FontSize,
                        title.Typography.Weight,
                        title.Typography.Alignment,
                        title.Typography.LineHeight,
                        title.Typography.LetterSpacing),
                })
                : entry.Value);

        return document with { Nodes = nodes };
    }
}
