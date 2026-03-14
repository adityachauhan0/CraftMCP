namespace CraftMCP.Domain.Validation;

public sealed record DocumentValidationResult(IReadOnlyList<DocumentValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}
