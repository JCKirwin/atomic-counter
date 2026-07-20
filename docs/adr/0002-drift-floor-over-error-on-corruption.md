# ADR 0002: Drift Floor Over Error on Corruption

## Context

When the counter file is missing or contains invalid data, the allocator has two options: throw an error and require manual intervention, or self-repair by scanning existing artifacts.

## Decision

Self-repair via drift-floor scanning. If the counter file is absent, empty, or contains a non-numeric value, treat the persisted value as zero and let the drift scanner determine the true baseline from artifacts on disk.

## Consequences

- The allocator never fails due to counter file corruption. It recovers silently and correctly.
- A full directory scan runs on every allocation (not just on recovery). This is conservative but ensures the counter never falls behind reality.
- If no artifacts exist and the counter file is corrupt, the counter restarts from 1. Old IDs issued before the corruption are lost to the gap — but never reused.
