# The Pattern

You need to hand out unique, sequential IDs to multiple concurrent processes. You have no database. You have a filesystem. This document explains the atomic counter pattern — a file-lock-guarded monotonic ID allocator that solves this problem with three interlocking ideas: mutual exclusion via file locks, drift-floor scanning, and atomic batch allocation.

## The problem

Imagine a library receiving donated books from three sources at once. Each book needs an accession number — a unique, permanently assigned integer that identifies it in the catalog. Three librarians are cataloging simultaneously. If two of them accidentally assign the same number, you have a collision. If the counter somehow rewinds, you have a duplicate. If the counter file gets deleted, you have chaos.

A database would solve this with an auto-increment column. But not every system has a database, and not every problem justifies one. Sometimes a shared directory and a counter file are all you have.

## File locking for mutual exclusion

The core mechanism is a file lock. Before any process reads or writes the counter file, it acquires an exclusive lock on it. The operating system guarantees that only one process holds the lock at a time. Every other process blocks until the lock is released.

The sequence looks like this:

1. Acquire an exclusive lock on the counter file.
2. Read the current value.
3. Increment it (or reserve a batch).
4. Write the new value back.
5. Release the lock.

Steps 2 through 4 happen inside the lock. No other process can read a stale value or write a conflicting one. The lock is the boundary of the atomic operation.

## Drift-floor scanning

File locks handle concurrency. They do not handle data loss. If the counter file is deleted, corrupted, or manually edited to a lower value, the allocator could reissue IDs that are already in use.

Drift-floor scanning prevents this. Before issuing a new ID, the allocator scans the directory of existing artifacts — in our case, the catalog card files — and finds the highest ID already present on disk. This is the drift floor. The allocator uses whichever is greater: the persisted counter value or the drift floor.

If the counter says 42 but a catalog card numbered 57 exists on disk, the next ID is 58. The counter self-corrects.

This makes the allocator resilient. You can delete the counter file entirely, and the next allocation will recover by scanning what already exists. No data is lost. No IDs are reissued.

## Atomic batch allocation

Sometimes you need more than one ID at a time. A librarian cataloging a box of ten books should not lock and unlock ten times. Batch allocation reserves a contiguous range of IDs in a single locked operation.

The process is the same as single allocation, but instead of incrementing by one, you increment by N. The allocator returns the range `[start, start + N)` and writes `start + N` as the new counter value. All N IDs are reserved atomically — either the entire batch succeeds or none of it does.

## The demo domain

This project uses a library catalog as its demo domain. Books receive accession numbers. Catalog cards are small JSON files written to a shared directory. Multiple librarian processes run concurrently, each requesting accession numbers from the same atomic counter.

The domain is deliberately simple. The pattern is the point, not the domain. Accession numbers are opaque integers — they carry no meaning beyond uniqueness and ordering.

## What the pattern does not do

A few explicit non-goals, so you know where the boundaries are:

- **No distributed consensus.** This pattern relies on local filesystem locks. It does not work across network shares or distributed systems without additional coordination.
- **No ID recycling.** Once an ID is issued, it is gone. Deleting a catalog card does not free its accession number.
- **No semantic encoding.** The IDs are integers. They do not encode dates, categories, or any other information. If you need that, layer it on top.
