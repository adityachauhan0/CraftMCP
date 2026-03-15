namespace CraftMCP.App.Models;

public sealed record WorkspaceActivityEntry(
    DateTimeOffset TimestampUtc,
    string Summary,
    WorkspaceActivitySeverity Severity,
    string? Detail = null);

public enum WorkspaceActivitySeverity
{
    Info,
    Warning,
    Error,
}
