using CraftMCP.Domain.Models;

namespace CraftMCP.Domain.Commands;

public static class CommandValidator
{
    public static CommandValidationResult Validate(DocumentState document, CommandBatch batch)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(batch);

        var processing = CommandEngineCore.Process(document, batch, includeInverses: false);
        return new CommandValidationResult(processing.Warnings, processing.Errors);
    }
}
