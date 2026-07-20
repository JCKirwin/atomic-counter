# Extending the Atomic Counter

This implementation is deliberately minimal. Here are the most common extensions and how to add them.

## Named counters (multi-sequence)

You might need separate ID sequences for different artifact types. Add a `CounterName` property to `AtomicCounterOptions` and use it to partition the counter file path:

```csharp
var path = Path.Combine(baseDir, $"counter-{options.CounterName}.txt");
```

Each sequence gets its own file and its own lock — no cross-sequence contention.

## Cached drift-floor scanning

If scanning the artifact directory on every allocation is too expensive, cache the drift floor in memory and only re-scan on recovery:

```csharp
private long? _cachedDriftFloor;

long driftFloor = _cachedDriftFloor
    ?? await _driftScanner.ScanForHighestIdAsync(cancellationToken);
_cachedDriftFloor = Math.Max(driftFloor, baseline + count);
```

Reset `_cachedDriftFloor` to `null` when you detect a missing or corrupted counter file.

## Event callbacks

Add an `Action<long, long>` callback to options, fired after each allocation with the old and new high-water marks:

```csharp
public Action<long, long>? OnAllocated { get; init; }
```

Useful for logging, metrics, or triggering downstream processes when IDs are assigned.

## Distributed locking

Replace `FileShare.None` with a distributed lock (Redis, ZooKeeper, etcd) to coordinate across machines. The `IAtomicCounter` interface stays the same — swap the implementation:

```csharp
public sealed class DistributedAtomicCounter : IAtomicCounter
{
    private readonly IDistributedLock _lock;
    // ...
}
```

You'll also need to move the counter state to a shared store (Redis key, database row) since a local file won't be visible to other machines.

## Reservation with expiry

For long-running workflows, you might want to reserve an ID range that expires if not confirmed:

```csharp
public Task<Reservation> ReserveAsync(int count, TimeSpan ttl);
public Task ConfirmAsync(Reservation reservation);
```

Unconfirmed reservations return their IDs to the pool after the TTL. This breaks the "no recycling" guarantee, so keep it behind a separate interface.

## Metrics integration

Expose counters via `System.Diagnostics.Metrics`:

```csharp
private readonly Counter<long> _allocationsTotal;
private readonly Histogram<double> _lockWaitMs;
```

Increment `_allocationsTotal` on each successful allocation. Record `_lockWaitMs` as the time spent in the retry loop. This integrates with OpenTelemetry without coupling to it.
