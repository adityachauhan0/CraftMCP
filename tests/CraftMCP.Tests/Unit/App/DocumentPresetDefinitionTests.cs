using CraftMCP.App.Models;

namespace CraftMCP.Tests.Unit.App;

public sealed class DocumentPresetDefinitionTests
{
    [Fact]
    public void ToString_ReturnsDisplayName()
    {
        var preset = DocumentPresetDefinition.BuiltIn.Single(definition => definition.Preset == CraftMCP.Domain.ValueObjects.CanvasPreset.SquarePost);

        Assert.Equal("Square Post", preset.ToString());
    }
}
