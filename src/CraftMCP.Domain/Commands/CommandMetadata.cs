using CraftMCP.Domain.Ids;

namespace CraftMCP.Domain.Commands;

public sealed record CommandProvenance
{
    public CommandProvenance(CommandSource source, string actor, string? correlationId = null, string? detail = null)
    {
        if (string.IsNullOrWhiteSpace(actor))
        {
            throw new ArgumentException("Command actor cannot be blank.", nameof(actor));
        }

        Source = source;
        Actor = actor;
        CorrelationId = correlationId;
        Detail = detail;
    }

    public CommandSource Source { get; init; }

    public string Actor { get; init; }

    public string? CorrelationId { get; init; }

    public string? Detail { get; init; }
}

public sealed record CommandWarning
{
    public CommandWarning(string code, string message, int? commandIndex = null, NodeId? nodeId = null, AssetId? assetId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Warning code cannot be blank.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Warning message cannot be blank.", nameof(message));
        }

        Code = code;
        Message = message;
        CommandIndex = commandIndex;
        NodeId = nodeId;
        AssetId = assetId;
    }

    public string Code { get; init; }

    public string Message { get; init; }

    public int? CommandIndex { get; init; }

    public NodeId? NodeId { get; init; }

    public AssetId? AssetId { get; init; }
}

public sealed record CommandFailure
{
    public CommandFailure(string code, string message, int? commandIndex = null, NodeId? nodeId = null, AssetId? assetId = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Failure code cannot be blank.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Failure message cannot be blank.", nameof(message));
        }

        Code = code;
        Message = message;
        CommandIndex = commandIndex;
        NodeId = nodeId;
        AssetId = assetId;
    }

    public string Code { get; init; }

    public string Message { get; init; }

    public int? CommandIndex { get; init; }

    public NodeId? NodeId { get; init; }

    public AssetId? AssetId { get; init; }
}

public enum CommandSource
{
    Human,
    Agent,
    System,
}
