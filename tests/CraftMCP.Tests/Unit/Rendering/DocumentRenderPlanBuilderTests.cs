using CraftMCP.Rendering.Scene;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.Rendering;

public sealed class DocumentRenderPlanBuilderTests
{
    [Fact]
    public void Build_UsesExplicitRootAndChildSceneOrderEvenWhenNodeRegistryIsShuffled()
    {
        var document = DocumentExportFixtureFactory.CreateSocialGraphic(shuffled: true);
        var builder = new DocumentRenderPlanBuilder();

        var plan = builder.Build(document);

        var drawableOrder = plan.Nodes
            .Where(node => node.IsDrawable)
            .Select(node => node.NodeId.Value)
            .ToArray();

        Assert.Equal(
            new[]
            {
                "node_social_background",
                "node_social_hero_image",
                "node_social_title",
                "node_social_subtitle",
                "node_social_cta_button",
                "node_social_cta_label",
            },
            drawableOrder);
    }
}
