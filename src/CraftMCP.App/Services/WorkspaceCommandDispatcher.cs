using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Models;

namespace CraftMCP.App.Services;

public sealed class WorkspaceCommandDispatcher
{
    public WorkspaceCommandDispatchResult Commit(
        DocumentState document,
        CommandHistoryStack history,
        string summary,
        IReadOnlyList<DesignCommand> commands,
        string actor,
        string? detail = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(commands);

        var batch = new CommandBatch(
            summary,
            commands,
            new CommandProvenance(CommandSource.Human, actor, detail: detail),
            null);

        return Commit(document, history, batch);
    }

    public WorkspaceCommandDispatchResult Commit(
        DocumentState document,
        CommandHistoryStack history,
        CommandBatch batch)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(batch);

        var outcome = history.Commit(document, batch);
        return new WorkspaceCommandDispatchResult(
            outcome.Document,
            outcome.History,
            outcome.Result,
            outcome.HistoryEntry);
    }

    public WorkspaceCommandDispatchResult Undo(DocumentState document, CommandHistoryStack history)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);

        var outcome = history.Undo(document);
        return new WorkspaceCommandDispatchResult(
            outcome.Document,
            outcome.History,
            outcome.Result,
            outcome.HistoryEntry);
    }

    public WorkspaceCommandDispatchResult Redo(DocumentState document, CommandHistoryStack history)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(history);

        var outcome = history.Redo(document);
        return new WorkspaceCommandDispatchResult(
            outcome.Document,
            outcome.History,
            outcome.Result,
            outcome.HistoryEntry);
    }
}

public sealed record WorkspaceCommandDispatchResult(
    DocumentState Document,
    CommandHistoryStack History,
    CommandResult Result,
    HistoryEntry? HistoryEntry);
