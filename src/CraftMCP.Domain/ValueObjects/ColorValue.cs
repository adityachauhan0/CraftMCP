namespace CraftMCP.Domain.ValueObjects;

public readonly record struct ColorValue(byte Red, byte Green, byte Blue, byte Alpha = 255);
