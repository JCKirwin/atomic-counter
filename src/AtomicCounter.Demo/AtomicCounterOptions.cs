namespace AtomicCounter.Demo;

/// <summary>
/// Configuration for the file-based atomic counter.
/// </summary>
public sealed class AtomicCounterOptions
{
    /// <summary>
    /// Path to the file that persists the current counter value.
    /// </summary>
    public required string CounterFilePath { get; init; }

    /// <summary>
    /// Directory containing existing artifacts, scanned to compute the drift floor.
    /// </summary>
    public required string ArtifactDirectory { get; init; }

    /// <summary>
    /// File extension filter used when scanning artifacts for drift detection.
    /// </summary>
    public string ArtifactExtension { get; init; } = ".json";
}
