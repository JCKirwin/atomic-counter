namespace AtomicCounter.Demo;

/// <summary>
/// Scans existing artifacts on disk to determine the highest ID already in use.
/// This drift floor prevents the counter from issuing duplicate IDs after state loss.
/// </summary>
public interface IDriftScanner
{
    /// <summary>
    /// Returns the highest ID found among existing artifacts, or zero if none exist.
    /// </summary>
    Task<long> ScanForHighestIdAsync(CancellationToken cancellationToken = default);
}
