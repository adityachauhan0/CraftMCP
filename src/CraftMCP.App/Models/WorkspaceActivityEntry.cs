namespace CraftMCP.App.Models;

public sealed record WorkspaceActivityEntry(
    DateTimeOffset TimestampUtc,
    string Summary,
    WorkspaceActivitySeverity Severity,
    string? Detail = null,
    string SourceLabel = "System",
    string? Actor = null)
{
    public string TimestampLabel => TimestampUtc.ToLocalTime().ToString("HH:mm:ss");
}

public enum WorkspaceActivitySeverity
{
    Info,
    Warning,
    Error,
}
