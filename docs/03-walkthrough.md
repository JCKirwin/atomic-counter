# Code Walkthrough

This walkthrough tours the source files in order, explaining what each piece does and how it connects to the atomic counter pattern.

## IAtomicCounter.cs

The interface defines two operations: `AllocateAsync` for a single ID, and `AllocateBatchAsync` for a contiguous block. Both return `Task`-based results because the underlying file I/O is inherently async-capable, even though the current implementation blocks on file locks.

## AtomicCounterOptions.cs

Configuration is a simple class with three properties: the path to the counter file, the directory of existing artifacts to scan, and a file extension filter for the drift scanner. The extension defaults to `.json`.

## IDriftScanner.cs / DriftScanner.cs

The drift scanner is the recovery mechanism. It scans the artifact directory for files matching the configured extension, extracts trailing digits from each filename using a source-generated regex, and returns the highest number found.

The key insight: even if the counter file is deleted or corrupted, the artifacts on disk are the ground truth. The scanner reads that truth so the counter never issues a duplicate.

```csharp
var match = AccessionNumberPattern().Match(fileName);
```

The `[GeneratedRegex]` attribute makes this a compile-time regex — no runtime parsing overhead.

## FileBasedAtomicCounter.cs

This is the core of the pattern. The allocation flow:

1. Open the counter file with `FileShare.None` — this is the mutex. No other process can open the file while you hold it.
2. Read the persisted high-water mark. If the file is empty or corrupted, treat it as zero.
3. Ask the drift scanner for the highest ID on disk.
4. Pick `Max(persisted, driftFloor)` as the baseline.
5. Allocate IDs starting from `baseline + 1`.
6. Write the new high-water mark and release the lock (via `Dispose`).

The retry loop around `new FileStream(...)` handles contention — if another process holds the lock, the caller waits with jitter before retrying.

`AllocateAsync` delegates to `AllocateBatchAsync(1)`, so both paths go through the same atomic code.

## CatalogCard.cs

A sealed record with five properties: `AccessionNumber`, `Title`, `Author`, `CatalogedBy`, and `CatalogedAt`. The accession number is the ID assigned by the atomic counter.

## ILibraryCatalog.cs / LibraryCatalog.cs

The library catalog is the demo-domain service. `CatalogItemAsync` requests an accession number from the counter, constructs a `CatalogCard`, serializes it as indented camelCase JSON, and writes it to disk as `card-{N}.json`.

`GetAllCardsAsync` reads all card files back and returns them sorted by accession number.

## Program.cs

The entry point runs a two-phase demo:

**Phase 1** launches three concurrent librarian tasks (Alice, Bob, Carol), each cataloging four books from a hardcoded list. After all finish, it verifies that all twelve accession numbers are unique and contiguous.

**Phase 2** simulates state loss by deleting the counter file. The next allocation recovers via drift-floor scanning — the counter reads the existing card files, finds the highest accession number, and continues from there. No duplicates, no errors.
