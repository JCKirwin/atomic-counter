namespace AtomicCounter.Demo.Tests;

/// <summary>
/// Shared helpers for creating isolated temp directories per test.
/// Each test gets its own directory tree so counter files and artifact
/// directories never collide across parallel test execution.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Creates a unique temp directory and returns a disposable handle
    /// that deletes the directory on dispose.
    /// </summary>
    public static TempTestDirectory CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "atomic-counter-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new TempTestDirectory(path);
    }

    /// <summary>
    /// Builds an AtomicCounterOptions pointing at counter.txt and an artifacts
    /// subdirectory inside the given root.
    /// </summary>
    public static AtomicCounterOptions BuildOptions(string rootDirectory)
    {
        var artifactDir = Path.Combine(rootDirectory, "artifacts");
        Directory.CreateDirectory(artifactDir);

        return new AtomicCounterOptions
        {
            CounterFilePath = Path.Combine(rootDirectory, "counter.txt"),
            ArtifactDirectory = artifactDir,
        };
    }
}

/// <summary>
/// Disposable wrapper around a temp directory. Cleans up on dispose.
/// </summary>
internal sealed class TempTestDirectory : IDisposable
{
    public string Path { get; }

    public TempTestDirectory(string path) => Path = path;

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup; CI runners may hold file locks briefly.
        }
    }
}
