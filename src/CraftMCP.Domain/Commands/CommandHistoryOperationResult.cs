using CraftMCP.Domain.Models;

namespace CraftMCP.Domain.Commands;

public sealed record CommandHistoryOperationResult(
    DocumentState Document,
    CommandHistoryStack History,
    CommandResult Result,
    HistoryEntry? HistoryEntry);
