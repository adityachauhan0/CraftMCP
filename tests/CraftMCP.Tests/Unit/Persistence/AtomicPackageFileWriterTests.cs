using CraftMCP.Persistence.Contracts;
using CraftMCP.Persistence.IO;

namespace CraftMCP.Tests.Unit.Persistence;

public sealed class AtomicPackageFileWriterTests
{
    [Fact]
    public void Write_LeavesExistingTargetUntouchedWhenWriteFails()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), $"craft-atomic-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        var targetPath = Path.Combine(tempDirectory, "document.craft");
        File.WriteAllText(targetPath, "original");
        var writer = new AtomicPackageFileWriter();

        try
        {
            var exception = Assert.Throws<CraftPackageException>(() => writer.Write(targetPath, stream =>
            {
                using var textWriter = new StreamWriter(stream, leaveOpen: true);
                textWriter.Write("partial");
                textWriter.Flush();
                throw new InvalidOperationException("boom");
            }));

            Assert.Equal("package_write_failed", exception.Code);
            Assert.Equal("original", File.ReadAllText(targetPath));
            Assert.Single(Directory.GetFiles(tempDirectory));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
