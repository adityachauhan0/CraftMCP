using System.Text.Json.Serialization;

namespace CraftMCP.Domain.ValueObjects;

public readonly record struct TypographyStyle
{
    [JsonConstructor]
    public TypographyStyle(string fontFamily, double fontSize, int weight, string alignment, double lineHeight, double letterSpacing)
    {
        if (string.IsNullOrWhiteSpace(fontFamily))
        {
            throw new ArgumentException("Font family cannot be blank.", nameof(fontFamily));
        }

        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(alignment))
        {
            throw new ArgumentException("Alignment cannot be blank.", nameof(alignment));
        }

        if (lineHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(lineHeight), "Line height must be greater than zero.");
        }

        FontFamily = fontFamily;
        FontSize = fontSize;
        Weight = weight;
        Alignment = alignment;
        LineHeight = lineHeight;
        LetterSpacing = letterSpacing;
    }

    public string FontFamily { get; }

    public double FontSize { get; }

    public int Weight { get; }

    public string Alignment { get; }

    public double LineHeight { get; }

    public double LetterSpacing { get; }
}
