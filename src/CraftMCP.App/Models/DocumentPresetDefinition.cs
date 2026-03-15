using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.App.Models;

public sealed record DocumentPresetDefinition(
    CanvasPreset Preset,
    string DisplayName,
    string DefaultDocumentName,
    double Width,
    double Height,
    ColorValue Background,
    SafeAreaInsets? SafeArea = null)
{
    public static IReadOnlyList<DocumentPresetDefinition> BuiltIn { get; } =
        new[]
        {
            new DocumentPresetDefinition(
                CanvasPreset.SquarePost,
                "Square Post",
                "Square Post",
                1080,
                1080,
                new ColorValue(245, 240, 232)),
            new DocumentPresetDefinition(
                CanvasPreset.Slide,
                "Presentation Slide",
                "Presentation Slide",
                1600,
                900,
                new ColorValue(18, 22, 33),
                new SafeAreaInsets(80, 60, 80, 60)),
            new DocumentPresetDefinition(
                CanvasPreset.DesktopFrame,
                "Desktop Frame",
                "Desktop Frame",
                1440,
                900,
                new ColorValue(241, 245, 249)),
            new DocumentPresetDefinition(
                CanvasPreset.MobileFrame,
                "Mobile Frame",
                "Mobile Frame",
                393,
                852,
                new ColorValue(255, 255, 255)),
            new DocumentPresetDefinition(
                CanvasPreset.Custom,
                "Custom",
                "Custom Canvas",
                1280,
                720,
                new ColorValue(255, 255, 255)),
        };
}
