namespace AtomicCounter.Demo.Tests;

/// <summary>
/// Invariant: No two callers ever receive the same ID, even under concurrent access.
/// </summary>
public sealed class UniquenessTests
{
    [Fact]
    public async Task SingleAllocator_SequentialCalls_ProduceUniqueIds()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var ids = new List<long>();
        for (int i = 0; i < 100; i++)
        {
            ids.Add(await counter.AllocateAsync());
        }

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task ConcurrentCallers_NeverReceiveDuplicateIds()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        const int taskCount = 10;
        const int allocationsPerTask = 20;

        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => Task.Run(async () =>
            {
                var localIds = new List<long>();
                for (int i = 0; i < allocationsPerTask; i++)
                {
                    localIds.Add(await counter.AllocateAsync());
                }
                return localIds;
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);
        var allIds = results.SelectMany(r => r).ToList();

        Assert.Equal(taskCount * allocationsPerTask, allIds.Count);
        Assert.Equal(allIds.Count, allIds.Distinct().Count());
    }

    [Fact]
    public async Task BatchAllocation_ProducesUniqueIdsAcrossCalls()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var batch1 = await counter.AllocateBatchAsync(5);
        var batch2 = await counter.AllocateBatchAsync(5);

        var allIds = batch1.Concat(batch2).ToList();
        Assert.Equal(10, allIds.Count);
        Assert.Equal(allIds.Count, allIds.Distinct().Count());
    }
}
