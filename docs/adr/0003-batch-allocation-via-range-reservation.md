# ADR 0003: Batch Allocation via Range Reservation

## Context

Some callers need multiple IDs at once. They could call `AllocateAsync` in a loop, but each call acquires and releases the file lock separately — unnecessary overhead and a window for interleaving.

## Decision

`AllocateBatchAsync(count)` reserves a contiguous range in a single locked operation. `AllocateAsync` is implemented as `AllocateBatchAsync(1)`, so both paths share the same atomic code.

## Consequences

- Batch allocation is atomic: all N IDs are reserved or none are. No partial allocations.
- The returned IDs are always contiguous (`baseline+1` through `baseline+N`), which callers can rely on for range-based processing.
- A single call holds the file lock for the same duration as a single allocation — the only difference is the arithmetic.
