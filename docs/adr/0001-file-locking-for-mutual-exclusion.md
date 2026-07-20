# ADR 0001: File Locking for Mutual Exclusion

## Context

The counter needs to prevent concurrent processes from reading and writing the counter file simultaneously. Options include a database lock, a named mutex, a `SemaphoreSlim`, or file-level locking via `FileShare.None`.

## Decision

Use `FileStream` opened with `FileShare.None`. The operating system enforces the lock — no additional runtime or infrastructure is needed. A retry loop with jitter handles contention.

## Consequences

- Works across processes on the same machine with zero dependencies beyond the filesystem.
- Does not work across machines — a distributed lock would be needed for that.
- `Thread.Sleep` in the retry loop blocks the calling thread. This is acceptable because the lock hold time is microseconds (read an integer, write an integer), so contention clears quickly.
