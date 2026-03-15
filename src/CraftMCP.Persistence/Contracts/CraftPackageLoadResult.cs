namespace CraftMCP.Persistence.Contracts;

public sealed record CraftPackageLoadResult(
    CraftPackageDocument Package,
    CraftPackageMetadata Metadata,
    IReadOnlyList<CraftPackageWarning> Warnings);
