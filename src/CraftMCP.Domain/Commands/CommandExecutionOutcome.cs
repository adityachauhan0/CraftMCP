using CraftMCP.Domain.Models;

namespace CraftMCP.Domain.Commands;

public sealed record CommandExecutionOutcome(
    DocumentState Document,
    CommandResult Result,
    HistoryEntry? HistoryEntry);
