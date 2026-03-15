using CraftMCP.Agent;
using CraftMCP.App.Models.Session;
using CraftMCP.App.Services;
using CraftMCP.Domain.Commands;
using CraftMCP.Domain.Ids;
using CraftMCP.Tests.TestSupport;

namespace CraftMCP.Tests.Unit.App;

public sealed class WorkspaceAgentServiceTests
{
    [Fact]
    public void CreateProposal_MarksInvalidPlannerBatchWithValidationFailures()
    {
        var service = new WorkspaceAgentService(new InvalidPlanner());
        var document = DocumentExportFixtureFactory.CreateUiMockup();

        var proposal = service.CreateProposal(
            document,
            new SelectionState(Array.Empty<NodeId>()),
            "Hide the missing node.");

        Assert.Equal(PlannerOutputStatus.Invalid, proposal.Status);
        Assert.NotNull(proposal.Batch);
        Assert.False(proposal.CanApprove);
        Assert.Contains(proposal.Errors, error => error.Code == "node_not_found");
    }

    private sealed class InvalidPlanner : IInternalPlanner
    {
        public PlannerOutput Plan(SceneContext context)
        {
            var batch = new CommandBatch(
                "Hide missing node",
                [new SetVisibilityCommand(NodeId.From("node_missing"), false)],
                new CommandProvenance(CommandSource.Agent, "planner:test", "proposal_invalid"),
                "Exercise the invalid planner-output state.");

            return new PlannerOutput(
                "proposal_invalid",
                PlannerOutputStatus.Ready,
                "Hide missing node",
                "Exercise the invalid planner-output state.",
                batch,
                Array.Empty<CommandWarning>(),
                Array.Empty<CommandFailure>());
        }
    }
}
