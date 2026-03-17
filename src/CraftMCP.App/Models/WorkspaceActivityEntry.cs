namespace CraftMCP.App.Models;

public sealed record WorkspaceActivityEntry(
    DateTimeOffset TimestampUtc,
    string Summary,
    WorkspaceActivitySeverity Severity,
    string? Detail = null,
    string SourceLabel = "System",
    string? Actor = null,
    string? ScopeLabel = null,
    string? CorrelationId = null)
{
    public string TimestampLabel => TimestampUtc.ToLocalTime().ToString("HH:mm:ss");

    public string SeverityLabel => Severity switch
    {
        WorkspaceActivitySeverity.Warning => "Warning",
        WorkspaceActivitySeverity.Error => "Error",
        _ => "Info",
    };

    public string MetaLabel =>
        string.Join(
            " • ",
            new[]
            {
                SeverityLabel,
                SourceLabel,
                Actor,
                CorrelationId,
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

    public string DetailText => string.IsNullOrWhiteSpace(Detail) ? "No additional detail." : Detail;
}

public enum WorkspaceActivitySeverity
{
    Info,
    Warning,
    Error,
}
