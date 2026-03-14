using CraftMCP.Domain.Ids;

namespace CraftMCP.Domain.Commands;

public sealed record CommandResult
{
    public CommandResult(
        bool isSuccess,
        IReadOnlyList<CommandWarning> warnings,
        IReadOnlyList<CommandFailure> errors,
        IReadOnlyList<NodeId> affectedNodeIds,
        IReadOnlyList<AssetId> affectedAssetIds,
        IReadOnlyList<DesignCommand> inverseCommands)
    {
        IsSuccess = isSuccess;
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        AffectedNodeIds = affectedNodeIds ?? throw new ArgumentNullException(nameof(affectedNodeIds));
        AffectedAssetIds = affectedAssetIds ?? throw new ArgumentNullException(nameof(affectedAssetIds));
        InverseCommands = inverseCommands ?? throw new ArgumentNullException(nameof(inverseCommands));
    }

    public bool IsSuccess { get; init; }

    public IReadOnlyList<CommandWarning> Warnings { get; init; }

    public IReadOnlyList<CommandFailure> Errors { get; init; }

    public IReadOnlyList<NodeId> AffectedNodeIds { get; init; }

    public IReadOnlyList<AssetId> AffectedAssetIds { get; init; }

    public IReadOnlyList<DesignCommand> InverseCommands { get; init; }
}
