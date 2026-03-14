namespace CraftMCP.Domain.Commands;

public sealed record CommandBatch
{
    public CommandBatch(
        string summary,
        IReadOnlyList<DesignCommand> commands,
        CommandProvenance provenance,
        string? rationale)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Command batch summary cannot be blank.", nameof(summary));
        }

        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(provenance);

        if (commands.Count == 0)
        {
            throw new ArgumentException("Command batch must contain at least one command.", nameof(commands));
        }

        Summary = summary;
        Commands = commands;
        Provenance = provenance;
        Rationale = rationale;
    }

    public string Summary { get; init; }

    public IReadOnlyList<DesignCommand> Commands { get; init; }

    public CommandProvenance Provenance { get; init; }

    public string? Rationale { get; init; }
}
