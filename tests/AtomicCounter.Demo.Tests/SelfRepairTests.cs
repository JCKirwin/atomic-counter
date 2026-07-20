using System.Text.Json;

namespace AtomicCounter.Demo.Tests;

/// <summary>
/// Invariant: A corrupted or missing counter file triggers a full scan and
/// self-repair rather than an error.
/// </summary>
public sealed class SelfRepairTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task MissingCounterFile_AllocatesFromDriftFloor()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        WriteStubCard(options.ArtifactDirectory, 25);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.True(id > 25, $"Expected ID > 25 (drift floor from artifacts), got {id}");
    }

    [Fact]
    public async Task MissingCounterFile_NoArtifacts_StartsFromOne()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.Equal(1L, id);
    }

    [Fact]
    public async Task CorruptedCounterFile_FallsBackToDriftFloor()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        await File.WriteAllTextAsync(options.CounterFilePath, "NOT_A_NUMBER");

        WriteStubCard(options.ArtifactDirectory, 30);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.True(id > 30, $"Expected ID > 30 after corrupted counter file, got {id}");
    }

    [Fact]
    public async Task NegativeValueInCounterFile_TreatedAsCorrupted()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        await File.WriteAllTextAsync(options.CounterFilePath, "-5");

        WriteStubCard(options.ArtifactDirectory, 10);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.True(id > 10, $"Expected ID > 10 after negative counter value, got {id}");
    }

    [Fact]
    public async Task DeletedCounterFile_MidSession_RecoversByScanning()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);

        var catalog = new LibraryCatalog(
            new FileBasedAtomicCounter(options, scanner), options);

        var card1 = await catalog.CatalogItemAsync("First Book", "Author A", "Librarian 1");
        var card2 = await catalog.CatalogItemAsync("Second Book", "Author B", "Librarian 1");

        File.Delete(options.CounterFilePath);

        var counterAfterDelete = new FileBasedAtomicCounter(options, new DriftScanner(options));
        var recoveredId = await counterAfterDelete.AllocateAsync();

        Assert.True(recoveredId > card2.AccessionNumber,
            $"Recovered ID {recoveredId} should be above last issued {card2.AccessionNumber}");
    }

    [Fact]
    public async Task EmptyCounterFile_TreatedAsFreshStart()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        await File.WriteAllTextAsync(options.CounterFilePath, "");

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.Equal(1L, id);
    }

    [Fact]
    public async Task CounterFile_RecreatedAfterRepair()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        WriteStubCard(options.ArtifactDirectory, 15);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        await counter.AllocateAsync();

        Assert.True(File.Exists(options.CounterFilePath), "Counter file should exist after allocation");

        var persisted = long.Parse(await File.ReadAllTextAsync(options.CounterFilePath));
        Assert.True(persisted >= 16, $"Persisted value {persisted} should be at least 16 (drift floor 15 + 1)");
    }

    private static void WriteStubCard(string artifactDirectory, long accessionNumber)
    {
        var card = new CatalogCard
        {
            AccessionNumber = accessionNumber,
            Title = $"Book {accessionNumber}",
            Author = "Test Author",
            CatalogedBy = "Test",
        };

        var path = Path.Combine(artifactDirectory, $"card-{accessionNumber}.json");
        var json = JsonSerializer.Serialize(card, JsonOptions);
        File.WriteAllText(path, json);
    }
}
