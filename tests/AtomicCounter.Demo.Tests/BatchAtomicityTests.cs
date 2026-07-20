namespace AtomicCounter.Demo.Tests;

/// <summary>
/// Invariant: Batch allocation of N IDs is atomic -- either all N are reserved or none are.
/// </summary>
public sealed class BatchAtomicityTests
{
    [Fact]
    public async Task BatchAllocation_ReturnsExactlyNIds()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var batch = await counter.AllocateBatchAsync(10);

        Assert.Equal(10, batch.Count);
    }

    [Fact]
    public async Task BatchAllocation_IdsAreContiguous()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var batch = await counter.AllocateBatchAsync(5);

        for (int i = 1; i < batch.Count; i++)
        {
            Assert.Equal(batch[i - 1] + 1, batch[i]);
        }
    }

    [Fact]
    public async Task BatchAllocation_UpdatesPersistedCounter_SoNextCallContinues()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var batch = await counter.AllocateBatchAsync(10);
        var nextId = await counter.AllocateAsync();

        Assert.Equal(batch[^1] + 1, nextId);
    }

    [Fact]
    public async Task BatchAllocation_WithZeroCount_Throws()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => counter.AllocateBatchAsync(0));
    }

    [Fact]
    public async Task BatchAllocation_WithNegativeCount_Throws()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => counter.AllocateBatchAsync(-1));
    }

    [Fact]
    public async Task ConcurrentBatches_NeverOverlap()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        const int taskCount = 8;
        const int batchSize = 10;

        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(() => counter.AllocateBatchAsync(batchSize)))
            .ToArray();

        var batches = await Task.WhenAll(tasks);
        var allIds = batches.SelectMany(b => b).ToList();

        Assert.Equal(taskCount * batchSize, allIds.Count);
        Assert.Equal(allIds.Count, allIds.Distinct().Count());
    }
}
