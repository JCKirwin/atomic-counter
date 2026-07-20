namespace AtomicCounter.Demo.Tests;

/// <summary>
/// Invariant: IDs are strictly monotonically increasing within a single allocator instance.
/// </summary>
public sealed class MonotonicityTests
{
    [Fact]
    public async Task SequentialAllocations_AreStrictlyIncreasing()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        long previous = 0;
        for (int i = 0; i < 50; i++)
        {
            var id = await counter.AllocateAsync();
            Assert.True(id > previous, $"ID {id} was not greater than previous {previous}");
            previous = id;
        }
    }

    [Fact]
    public async Task BatchAllocations_ReturnStrictlyIncreasingSequence()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var batch = await counter.AllocateBatchAsync(20);

        for (int i = 1; i < batch.Count; i++)
        {
            Assert.True(batch[i] > batch[i - 1],
                $"Batch ID at index {i} ({batch[i]}) was not greater than index {i - 1} ({batch[i - 1]})");
        }
    }

    [Fact]
    public async Task ConsecutiveBatches_MaintainMonotonicity()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var batch1 = await counter.AllocateBatchAsync(5);
        var batch2 = await counter.AllocateBatchAsync(5);

        Assert.True(batch2[0] > batch1[^1],
            $"First ID of batch2 ({batch2[0]}) should be greater than last ID of batch1 ({batch1[^1]})");
    }
}
