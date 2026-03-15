using CraftMCP.Domain.Ids;

namespace CraftMCP.Persistence.Contracts;

public sealed record CraftPackageWarning(string Code, string Message, AssetId? AssetId = null, string? PackagePath = null);
