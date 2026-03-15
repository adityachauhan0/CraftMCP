using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;

namespace CraftMCP.Domain.Commands;

public sealed record CommandHistoryStack
{
    public static CommandHistoryStack Empty { get; } = new(Array.Empty<HistoryEntry>(), Array.Empty<HistoryEntry>());

    public CommandHistoryStack(IReadOnlyList<HistoryEntry> undoEntries, IReadOnlyList<HistoryEntry> redoEntries)
    {
        UndoEntries = undoEntries ?? throw new ArgumentNullException(nameof(undoEntries));
        RedoEntries = redoEntries ?? throw new ArgumentNullException(nameof(redoEntries));
    }

    public IReadOnlyList<HistoryEntry> UndoEntries { get; init; }

    public IReadOnlyList<HistoryEntry> RedoEntries { get; init; }

    public CommandHistoryOperationResult Commit(DocumentState document, CommandBatch batch)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(batch);

        var outcome = CommandExecutor.Execute(document, batch);
        if (!outcome.Result.IsSuccess || outcome.HistoryEntry is null)
        {
            return new CommandHistoryOperationResult(document, this, outcome.Result, null);
        }

        var undoEntries = UndoEntries.ToList();
        undoEntries.Add(outcome.HistoryEntry);

        return new CommandHistoryOperationResult(
            outcome.Document,
            new CommandHistoryStack(undoEntries, Array.Empty<HistoryEntry>()),
            outcome.Result,
            outcome.HistoryEntry);
    }

    public CommandHistoryOperationResult Undo(DocumentState document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (UndoEntries.Count == 0)
        {
            return CreateNoopResult(document, "undo_stack_empty", "There is no committed batch to undo.");
        }

        var targetEntry = UndoEntries[^1];
        var batch = new CommandBatch(
            $"Undo: {targetEntry.Batch.Summary}",
            targetEntry.Result.InverseCommands,
            new CommandProvenance(CommandSource.System, "history:undo", targetEntry.EntryId),
            null);

        var outcome = CommandExecutor.Execute(document, batch);
        if (!outcome.Result.IsSuccess || outcome.HistoryEntry is null)
        {
            return new CommandHistoryOperationResult(document, this, outcome.Result, null);
        }

        var undoEntries = UndoEntries.Take(UndoEntries.Count - 1).ToArray();
        var redoEntries = RedoEntries.ToList();
        redoEntries.Add(outcome.HistoryEntry);

        return new CommandHistoryOperationResult(
            outcome.Document,
            new CommandHistoryStack(undoEntries, redoEntries),
            outcome.Result,
            outcome.HistoryEntry);
    }

    public CommandHistoryOperationResult Redo(DocumentState document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (RedoEntries.Count == 0)
        {
            return CreateNoopResult(document, "redo_stack_empty", "There is no committed batch to redo.");
        }

        var targetEntry = RedoEntries[^1];
        var batch = new CommandBatch(
            $"Redo: {targetEntry.Batch.Summary}",
            targetEntry.Result.InverseCommands,
            new CommandProvenance(CommandSource.System, "history:redo", targetEntry.EntryId),
            null);

        var outcome = CommandExecutor.Execute(document, batch);
        if (!outcome.Result.IsSuccess || outcome.HistoryEntry is null)
        {
            return new CommandHistoryOperationResult(document, this, outcome.Result, null);
        }

        var redoEntries = RedoEntries.Take(RedoEntries.Count - 1).ToArray();
        var undoEntries = UndoEntries.ToList();
        undoEntries.Add(outcome.HistoryEntry);

        return new CommandHistoryOperationResult(
            outcome.Document,
            new CommandHistoryStack(undoEntries, redoEntries),
            outcome.Result,
            outcome.HistoryEntry);
    }

    private CommandHistoryOperationResult CreateNoopResult(DocumentState document, string code, string message) =>
        new(
            document,
            this,
            new CommandResult(
                false,
                Array.Empty<CommandWarning>(),
                new[] { new CommandFailure(code, message) },
                Array.Empty<NodeId>(),
                Array.Empty<AssetId>(),
                Array.Empty<DesignCommand>()),
            null);
}
