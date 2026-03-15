using System.Text.RegularExpressions;
using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Models;
using CraftMCP.Domain.ValueObjects;

namespace CraftMCP.Agent;

public sealed class LocalKeywordPlanner : IInternalPlanner
{
    private static readonly Regex HexColorRegex = new("#(?:[0-9a-fA-F]{6}|[0-9a-fA-F]{8})", RegexOptions.Compiled);

    public PlannerOutput Plan(SceneContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var prompt = context.Prompt.Trim();
        if (prompt.Length == 0)
        {
            return Invalid("Prompt is blank.", "prompt_blank");
        }

        var proposalId = $"proposal_{Guid.NewGuid():N}";
        var loweredPrompt = prompt.ToLowerInvariant();

        if ((loweredPrompt.Contains("canvas") || loweredPrompt.Contains("background"))
            && TryParseColor(prompt, out var background))
        {
            var canvas = new CanvasModel(
                context.Canvas.Width,
                context.Canvas.Height,
                context.Canvas.Preset,
                background,
                context.Canvas.SafeArea);

            return Ready(
                proposalId,
                "Update canvas background",
                "Apply the requested background color through the shared canvas command.",
                [new SetCanvasCommand(canvas)]);
        }

        if (context.Selection.SelectedNodeIds.Count == 0)
        {
            return Invalid("Select at least one node before asking the planner to edit it.", "selection_required");
        }

        if (loweredPrompt.Contains("hide"))
        {
            return Ready(
                proposalId,
                "Hide selected nodes",
                "Turn off visibility for the selected nodes after review.",
                context.Selection.SelectedNodeIds.Select(nodeId => (DesignCommand)new SetVisibilityCommand(nodeId, false)).ToArray());
        }

        if (loweredPrompt.Contains("show"))
        {
            return Ready(
                proposalId,
                "Show selected nodes",
                "Restore visibility for the selected nodes after review.",
                context.Selection.SelectedNodeIds.Select(nodeId => (DesignCommand)new SetVisibilityCommand(nodeId, true)).ToArray());
        }

        if (loweredPrompt.Contains("unlock"))
        {
            return Ready(
                proposalId,
                "Unlock selected nodes",
                "Unlock the selected nodes after review.",
                context.Selection.SelectedNodeIds.Select(nodeId => (DesignCommand)new SetLockStateCommand(nodeId, false)).ToArray());
        }

        if (loweredPrompt.Contains("lock"))
        {
            return Ready(
                proposalId,
                "Lock selected nodes",
                "Lock the selected nodes after review.",
                context.Selection.SelectedNodeIds.Select(nodeId => (DesignCommand)new SetLockStateCommand(nodeId, true)).ToArray());
        }

        return Invalid("The local planner could not map that prompt to a reviewable command batch.", "unsupported_prompt");
    }

    private static PlannerOutput Ready(
        string proposalId,
        string summary,
        string rationale,
        IReadOnlyList<DesignCommand> commands)
    {
        var batch = new CommandBatch(
            summary,
            commands,
            new CommandProvenance(CommandSource.Agent, "planner:local", proposalId, rationale),
            rationale);

        return new PlannerOutput(
            proposalId,
            PlannerOutputStatus.Ready,
            summary,
            rationale,
            batch,
            Array.Empty<CommandWarning>(),
            Array.Empty<CommandFailure>());
    }

    private static PlannerOutput Invalid(string message, string code) =>
        new(
            $"proposal_{Guid.NewGuid():N}",
            PlannerOutputStatus.Invalid,
            "Proposal unavailable",
            message,
            null,
            Array.Empty<CommandWarning>(),
            [new CommandFailure(code, message)]);

    private static bool TryParseColor(string prompt, out ColorValue color)
    {
        color = default;
        var match = HexColorRegex.Match(prompt);
        if (!match.Success)
        {
            return false;
        }

        var trimmed = match.Value[1..];
        if (trimmed.Length == 6
            && byte.TryParse(trimmed[..2], System.Globalization.NumberStyles.HexNumber, null, out var red)
            && byte.TryParse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var green)
            && byte.TryParse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var blue))
        {
            color = new ColorValue(red, green, blue);
            return true;
        }

        if (trimmed.Length == 8
            && byte.TryParse(trimmed[..2], System.Globalization.NumberStyles.HexNumber, null, out var alpha)
            && byte.TryParse(trimmed.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, null, out red)
            && byte.TryParse(trimmed.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, null, out green)
            && byte.TryParse(trimmed.Substring(6, 2), System.Globalization.NumberStyles.HexNumber, null, out blue))
        {
            color = new ColorValue(red, green, blue, alpha);
            return true;
        }

        return false;
    }
}
