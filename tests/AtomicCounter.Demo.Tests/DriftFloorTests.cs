using System.Text.Json;

namespace AtomicCounter.Demo.Tests;

/// <summary>
/// Invariant: The counter never issues an ID less than or equal to any existing
/// artifact's ID (drift-floor guarantee).
/// </summary>
public sealed class DriftFloorTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public async Task NewCounter_WithExistingArtifacts_StartsAboveHighestArtifactId()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        WriteStubCard(options.ArtifactDirectory, 42);
        WriteStubCard(options.ArtifactDirectory, 99);
        WriteStubCard(options.ArtifactDirectory, 7);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.True(id > 99, $"Expected ID > 99 (highest artifact), got {id}");
    }

    [Fact]
    public async Task Counter_WithPersistedValueBelowDriftFloor_UsesFloorInstead()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        await File.WriteAllTextAsync(options.CounterFilePath, "10");

        WriteStubCard(options.ArtifactDirectory, 50);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.True(id > 50, $"Expected ID > 50 (drift floor), got {id}");
    }

    [Fact]
    public async Task Counter_WithPersistedValueAboveDriftFloor_UsesPersistedValue()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);

        await File.WriteAllTextAsync(options.CounterFilePath, "200");

        WriteStubCard(options.ArtifactDirectory, 50);

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);

        var id = await counter.AllocateAsync();

        Assert.True(id > 200, $"Expected ID > 200 (persisted value), got {id}");
    }

    [Fact]
    public async Task DriftScanner_WithNoArtifacts_ReturnsZero()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = TestHelpers.BuildOptions(tmp.Path);
        var scanner = new DriftScanner(options);

        var highest = await scanner.ScanForHighestIdAsync();

        Assert.Equal(0L, highest);
    }

    [Fact]
    public async Task DriftScanner_WithMissingDirectory_ReturnsZero()
    {
        using var tmp = TestHelpers.CreateTempDirectory();
        var options = new AtomicCounterOptions
        {
            CounterFilePath = Path.Combine(tmp.Path, "counter.txt"),
            ArtifactDirectory = Path.Combine(tmp.Path, "nonexistent"),
        };
        var scanner = new DriftScanner(options);

        var highest = await scanner.ScanForHighestIdAsync();

        Assert.Equal(0L, highest);
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
