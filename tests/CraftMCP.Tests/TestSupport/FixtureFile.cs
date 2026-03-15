namespace CraftMCP.Tests.TestSupport;

internal static class FixtureFile
{
    public static string ReadJson(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "json", fileName);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }

    public static byte[] ReadCraftAsset(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "craft", "assets", fileName);
        return File.ReadAllBytes(path);
    }

    public static string CraftPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "craft", fileName);
}
