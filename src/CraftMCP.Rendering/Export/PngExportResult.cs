namespace CraftMCP.Rendering.Export;

public sealed record PngExportResult(
    byte[] PngBytes,
    IReadOnlyList<RenderWarning> Warnings);
