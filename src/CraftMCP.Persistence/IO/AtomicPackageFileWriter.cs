using CraftMCP.Persistence.Contracts;

namespace CraftMCP.Persistence.IO;

public sealed class AtomicPackageFileWriter
{
    public void Write(string targetPath, Action<Stream> writeAction)
    {
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            throw new ArgumentException("Target path cannot be blank.", nameof(targetPath));
        }

        ArgumentNullException.ThrowIfNull(writeAction);

        var fullTargetPath = Path.GetFullPath(targetPath);
        var directory = Path.GetDirectoryName(fullTargetPath)
            ?? throw new InvalidOperationException($"Unable to resolve directory for '{fullTargetPath}'.");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(directory, $"{Path.GetFileName(fullTargetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var tempStream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                writeAction(tempStream);
                tempStream.Flush(flushToDisk: true);
            }

            if (File.Exists(fullTargetPath))
            {
                File.Replace(tempPath, fullTargetPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, fullTargetPath);
            }
        }
        catch (Exception exception) when (exception is not CraftPackageException)
        {
            TryDelete(tempPath);
            throw new CraftPackageException(
                "package_write_failed",
                $"Failed to write package '{fullTargetPath}'.",
                fullTargetPath,
                exception);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }
}
