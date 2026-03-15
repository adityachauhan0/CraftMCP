namespace CraftMCP.Agent;

public interface IInternalPlanner
{
    PlannerOutput Plan(SceneContext context);
}
