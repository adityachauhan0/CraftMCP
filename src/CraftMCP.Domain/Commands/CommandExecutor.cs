using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Models;

namespace CraftMCP.Domain.Commands;

public static class CommandExecutor
{
    public static CommandExecutionOutcome Execute(DocumentState document, CommandBatch batch)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(batch);

        var processing = CommandEngineCore.Process(document, batch, includeInverses: true);
        if (!processing.IsSuccess || processing.Document is null)
        {
            return new CommandExecutionOutcome(
                document,
                new CommandResult(
                    false,
                    processing.Warnings,
                    processing.Errors,
                    Array.Empty<NodeId>(),
                    Array.Empty<AssetId>(),
                    Array.Empty<DesignCommand>()),
                null);
        }

        var result = new CommandResult(
            true,
            processing.Warnings,
            Array.Empty<CommandFailure>(),
            processing.AffectedNodeIds,
            processing.AffectedAssetIds,
            processing.InverseCommands);
        var historyEntry = new HistoryEntry(
            $"history_{Guid.NewGuid():N}",
            DateTimeOffset.UtcNow,
            batch,
            result);

        return new CommandExecutionOutcome(processing.Document, result, historyEntry);
    }
}
