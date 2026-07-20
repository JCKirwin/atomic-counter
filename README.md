# Atomic Counter

A file-lock-guarded monotonic ID allocator in C# that guarantees unique, strictly increasing identifiers across concurrent processes — no database required. Fork it, read it, adapt it.

## What you'll learn

- How to use `FileShare.None` as a cross-process mutex
- How drift-floor scanning recovers from counter file corruption or deletion
- How to make batch allocation atomic (all-or-nothing range reservation)
- How to test concurrent file-locked code with deterministic assertions
- How to structure a self-repairing stateful component

## Quick Start

```bash
git clone https://github.com/JCKirwin/atomic-counter.git
cd atomic-counter
dotnet run --project src/AtomicCounter.Demo
```

The demo simulates three concurrent librarians cataloging book donations. Each librarian gets unique accession numbers from the shared counter. After all finish, an auditor deletes the counter file to demonstrate drift-floor recovery.

```
=== Atomic Counter Demo: Library Catalog ===
  [Alice] Cataloged: #1 "The Midnight Garden" by Elena Voss
  [Bob]   Cataloged: #2 "Lanterns Below" by Priya Nair
  [Carol] Cataloged: #3 "Raincatcher" by Liam O'Donnell
  ...
PASS: All accession numbers are unique.
PASS: Accession numbers form a contiguous sequence.
```

## Run the tests

```bash
dotnet test tests/AtomicCounter.Demo.Tests
```

## Project structure

```
├── docs/           Pattern explainer, architecture, tradeoffs, ADRs
├── src/            The atomic counter + library catalog demo
├── tests/          xUnit v3 tests covering every invariant
├── samples/        Demo configuration data
└── .github/        CI workflow (build + test on push/PR)
```

## License

MIT — see [LICENSE](LICENSE).
