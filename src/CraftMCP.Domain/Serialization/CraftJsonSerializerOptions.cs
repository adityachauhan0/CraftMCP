using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace CraftMCP.Domain.Serialization;

public static class CraftJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; } = CreateDefault();

    public static JsonSerializerOptions Export { get; } = CreateExport();

    private static JsonSerializerOptions CreateDefault()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = false,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            WriteIndented = false,
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.MakeReadOnly();
        return options;
    }

    private static JsonSerializerOptions CreateExport()
    {
        var options = new JsonSerializerOptions(Default)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
        };

        options.MakeReadOnly();
        return options;
    }
}
