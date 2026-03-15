using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Ids;
using CraftMCP.Domain.Nodes;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Agent;

public sealed record SceneContext(
    string Prompt,
    SceneCanvasContext Canvas,
    SelectedNodeContext Selection,
    IReadOnlyList<HierarchyNodeSummary> Hierarchy,
    IReadOnlyList<SceneNodeSummary> NodeSummaries);

public sealed record SceneCanvasContext(
    double Width,
    double Height,
    CanvasPreset Preset,
    ColorValue Background,
    SafeAreaInsets? SafeArea);

public sealed record SelectedNodeContext(
    IReadOnlyList<NodeId> SelectedNodeIds,
    NodeId? PrimaryNodeId,
    IReadOnlyList<SceneNodeSummary> SelectedNodes);

public sealed record HierarchyNodeSummary(
    NodeId NodeId,
    string Name,
    NodeKind Kind,
    NodeId? ParentId,
    IReadOnlyList<NodeId> ChildNodeIds);

public sealed record SceneNodeSummary(
    NodeId NodeId,
    string Name,
    NodeKind Kind,
    NodeId? ParentId,
    TransformValue Transform,
    bool IsVisible,
    bool IsLocked,
    OpacityValue Opacity,
    SizeValue? Size,
    RectValue? Bounds,
    PointValue? Start,
    PointValue? End,
    ColorValue? Fill,
    StrokeStyle? Stroke,
    double? CornerRadius,
    string? TextContent,
    TypographyStyle? Typography,
    AssetId? AssetId,
    string? FitMode,
    IReadOnlyList<NodeId> ChildNodeIds);

public enum PlannerOutputStatus
{
    Ready,
    Invalid,
    Failed,
}

public sealed record PlannerOutput
{
    public PlannerOutput(
        string proposalId,
        PlannerOutputStatus status,
        string summary,
        string? rationale,
        CommandBatch? batch,
        IReadOnlyList<CommandWarning> warnings,
        IReadOnlyList<CommandFailure> errors)
    {
        if (string.IsNullOrWhiteSpace(proposalId))
        {
            throw new ArgumentException("Proposal ID cannot be blank.", nameof(proposalId));
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            throw new ArgumentException("Proposal summary cannot be blank.", nameof(summary));
        }

        ProposalId = proposalId;
        Status = status;
        Summary = summary;
        Rationale = rationale;
        Batch = batch;
        Warnings = warnings ?? throw new ArgumentNullException(nameof(warnings));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public string ProposalId { get; init; }

    public PlannerOutputStatus Status { get; init; }

    public string Summary { get; init; }

    public string? Rationale { get; init; }

    public CommandBatch? Batch { get; init; }

    public IReadOnlyList<CommandWarning> Warnings { get; init; }

    public IReadOnlyList<CommandFailure> Errors { get; init; }

    public bool CanApprove => Status == PlannerOutputStatus.Ready && Batch is not null && Errors.Count == 0;
}
