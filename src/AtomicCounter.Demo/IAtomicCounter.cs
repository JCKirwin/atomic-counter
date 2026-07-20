namespace AtomicCounter.Demo;

/// <summary>
/// Allocates monotonically increasing integer IDs, guarded by file-level locking.
/// </summary>
public interface IAtomicCounter
{
    /// <summary>
    /// Reserves and returns the next available ID.
    /// </summary>
    Task<long> AllocateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically reserves a contiguous batch of IDs and returns them in order.
    /// </summary>
    Task<IReadOnlyList<long>> AllocateBatchAsync(int count, CancellationToken cancellationToken = default);
}
