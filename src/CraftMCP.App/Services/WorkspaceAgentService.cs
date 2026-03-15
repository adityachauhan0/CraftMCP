using CraftMCP.Agent;
using CraftMCP.App.Models.Session;
using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Models;

namespace CraftMCP.App.Services;

public sealed class WorkspaceAgentService
{
    private readonly IInternalPlanner _planner;

    public WorkspaceAgentService()
        : this(new LocalKeywordPlanner())
    {
    }

    public WorkspaceAgentService(IInternalPlanner planner)
    {
        _planner = planner ?? throw new ArgumentNullException(nameof(planner));
    }

    public PlannerOutput CreateProposal(
        DocumentState document,
        SelectionState selection,
        string prompt)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selection);

        if (string.IsNullOrWhiteSpace(prompt))
        {
            return new PlannerOutput(
                $"proposal_{Guid.NewGuid():N}",
                PlannerOutputStatus.Invalid,
                "Proposal unavailable",
                "Enter a prompt before submitting to the planner.",
                null,
                Array.Empty<CommandWarning>(),
                [new CommandFailure("prompt_blank", "Enter a prompt before submitting to the planner.")]);
        }

        try
        {
            var context = SceneContextFactory.Create(document, prompt, selection.SelectedNodeIds);
            var proposal = _planner.Plan(context);

            if (proposal.Batch is null)
            {
                return proposal;
            }

            var validation = CommandValidator.Validate(document, proposal.Batch);
            if (validation.IsValid && proposal.Status == PlannerOutputStatus.Ready)
            {
                return proposal with
                {
                    Warnings = proposal.Warnings.Concat(validation.Warnings).ToArray(),
                };
            }

            return proposal with
            {
                Status = PlannerOutputStatus.Invalid,
                Warnings = proposal.Warnings.Concat(validation.Warnings).ToArray(),
                Errors = proposal.Errors.Concat(validation.Errors).ToArray(),
            };
        }
        catch (Exception ex)
        {
            return new PlannerOutput(
                $"proposal_{Guid.NewGuid():N}",
                PlannerOutputStatus.Failed,
                "Proposal failed",
                "The planner could not produce a proposal.",
                null,
                Array.Empty<CommandWarning>(),
                [new CommandFailure("planner_failure", ex.Message)]);
        }
    }
}
