namespace CraftMCP.Persistence.Contracts;

public sealed class CraftPackageException : Exception
{
    public CraftPackageException(string code, string message, string? targetPath = null, Exception? innerException = null)
        : base(message, innerException)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Exception code cannot be blank.", nameof(code));
        }

        Code = code;
        TargetPath = targetPath;
    }

    public string Code { get; }

    public string? TargetPath { get; }
}
