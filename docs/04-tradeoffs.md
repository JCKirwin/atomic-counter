# Tradeoffs

Every design choice trades something. This page makes the tradeoffs explicit so you can decide whether they fit your context.

## File locking vs database sequences

This implementation uses `FileShare.None` on a `FileStream` as the mutual exclusion primitive. A database sequence (PostgreSQL `SERIAL`, MySQL `AUTO_INCREMENT`) would handle concurrency without explicit locking, but it adds an infrastructure dependency. File locking works anywhere a filesystem exists.

**Choose a database sequence if:** you already have a database in your stack and want the ID allocator to participate in transactions.

## Retry with jitter vs SemaphoreSlim

When the counter file is locked by another process, this implementation retries with randomized backoff (`Thread.Sleep` with jitter). An in-process `SemaphoreSlim` would be more efficient for threads within a single process, but it wouldn't protect against concurrent processes.

**Choose SemaphoreSlim if:** all callers are threads in one process and you don't need cross-process safety.

## Drift-floor scan on every allocation

The current implementation scans the artifact directory on every allocation, not just on recovery. This is conservative — it guarantees the counter never falls behind reality, even if another process writes artifacts outside the counter's knowledge.

The cost is one directory scan per allocation. For directories with thousands of files, this could be noticeable.

**Choose scan-on-recovery-only if:** you control all artifact creation and can guarantee no writes happen outside the counter. Cache the drift floor after the first scan and skip subsequent scans.

## No ID recycling

Deleted artifacts do not free their IDs. The counter only moves forward. This simplifies the design but means ID space is consumed permanently.

**Choose recycling if:** your ID space is bounded and you expect high churn. You'll need a free-list structure alongside the counter.

## Synchronous file locking in async methods

`AllocateAsync` uses `Thread.Sleep` in the retry loop rather than `Task.Delay`. This is deliberate — `FileStream` opened with `FileShare.None` is inherently synchronous; mixing `await` with file locks risks releasing the lock on a different thread than the one that acquired it.

**Choose fully async if:** you replace file locking with a `SemaphoreSlim` or distributed lock that supports async acquisition.

## Long values for IDs

IDs are `long` (64-bit), giving a theoretical ceiling of 9.2 quintillion. For most applications this is effectively infinite. Using `int` would halve the counter file size but cap at 2.1 billion.

**Choose int if:** storage is extremely constrained and you're confident the ID space won't exceed 2 billion.
