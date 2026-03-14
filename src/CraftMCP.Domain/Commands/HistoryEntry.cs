namespace CraftMCP.Domain.Commands;

public sealed record HistoryEntry
{
    public HistoryEntry(
        string entryId,
        DateTimeOffset appliedAtUtc,
        CommandBatch batch,
        CommandResult result)
    {
        if (string.IsNullOrWhiteSpace(entryId))
        {
            throw new ArgumentException("History entry ID cannot be blank.", nameof(entryId));
        }

        EntryId = entryId;
        AppliedAtUtc = appliedAtUtc;
        Batch = batch ?? throw new ArgumentNullException(nameof(batch));
        Result = result ?? throw new ArgumentNullException(nameof(result));
    }

    public string EntryId { get; init; }

    public DateTimeOffset AppliedAtUtc { get; init; }

    public CommandBatch Batch { get; init; }

    public CommandResult Result { get; init; }
}
