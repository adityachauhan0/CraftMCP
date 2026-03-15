namespace CraftMCP.Domain.Commands;

public sealed record CommandValidationResult(
    IReadOnlyList<CommandWarning> Warnings,
    IReadOnlyList<CommandFailure> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
