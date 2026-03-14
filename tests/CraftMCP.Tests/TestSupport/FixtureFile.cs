namespace CraftMCP.Tests.TestSupport;

internal static class FixtureFile
{
    public static string ReadJson(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "json", fileName);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
