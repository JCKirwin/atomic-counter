namespace AtomicCounter.Demo;

/// <summary>
/// Entry point for the atomic-counter demo. Simulates concurrent librarians
/// cataloging book donations, then verifies uniqueness and drift-floor recovery.
/// </summary>
public static class Program
{
    private static readonly (string Title, string Author)[] BookDonations =
    [
        ("The Midnight Garden", "Elena Voss"),
        ("Tides of Iron", "Marcus Hale"),
        ("Windmill Sonnets", "Ava Chen"),
        ("The Copper Fox", "Diego Ruiz"),
        ("Lanterns Below", "Priya Nair"),
        ("Echoes in Granite", "Sven Johansson"),
        ("The Quilted Map", "Fiona Byrne"),
        ("Salt and Sagebrush", "Tomoko Ishida"),
        ("Raincatcher", "Liam O'Donnell"),
        ("The Starling Hypothesis", "Amara Osei"),
        ("Blueprints for Silence", "Henrik Braun"),
        ("Foxglove Lane", "Rosalind Park"),
    ];

    public static async Task Main(string[] args)
    {
        var baseDir = Path.Combine(Path.GetTempPath(), $"atomic-counter-demo-{Guid.NewGuid():N}");
        var artifactDir = Path.Combine(baseDir, "catalog-cards");
        var counterFile = Path.Combine(baseDir, "counter.txt");

        Directory.CreateDirectory(artifactDir);

        var options = new AtomicCounterOptions
        {
            CounterFilePath = counterFile,
            ArtifactDirectory = artifactDir,
        };

        Console.WriteLine("=== Atomic Counter Demo: Library Catalog ===");
        Console.WriteLine($"Working directory: {baseDir}");
        Console.WriteLine();

        await RunConcurrentLibrarians(options);
        await RunDriftFloorRecovery(options, counterFile);

        Console.WriteLine();
        Console.WriteLine("=== Demo complete. Cleaning up... ===");

        try
        {
            Directory.Delete(baseDir, recursive: true);
        }
        catch
        {
            Console.WriteLine($"(Cleanup note: could not delete {baseDir})");
        }
    }

    private static async Task RunConcurrentLibrarians(AtomicCounterOptions options)
    {
        Console.WriteLine("--- Phase 1: Three concurrent librarians cataloging books ---");

        string[] librarians = ["Alice", "Bob", "Carol"];
        int booksPerLibrarian = BookDonations.Length / librarians.Length;

        var tasks = new List<Task<List<CatalogCard>>>();

        for (int i = 0; i < librarians.Length; i++)
        {
            var name = librarians[i];
            var startIndex = i * booksPerLibrarian;
            var books = BookDonations.Skip(startIndex).Take(booksPerLibrarian).ToArray();

            tasks.Add(Task.Run(async () =>
            {
                var scanner = new DriftScanner(options);
                var counter = new FileBasedAtomicCounter(options, scanner);
                var catalog = new LibraryCatalog(counter, options);
                var results = new List<CatalogCard>();

                foreach (var (title, author) in books)
                {
                    var card = await catalog.CatalogItemAsync(title, author, name);
                    Console.WriteLine($"  [{name}] Cataloged: #{card.AccessionNumber} \"{card.Title}\" by {card.Author}");
                    results.Add(card);
                }

                return results;
            }));
        }

        var allResults = await Task.WhenAll(tasks);
        var allCards = allResults.SelectMany(r => r).ToList();

        Console.WriteLine();
        Console.WriteLine($"Total cards created: {allCards.Count}");

        var accessionNumbers = allCards.Select(c => c.AccessionNumber).ToList();
        var uniqueNumbers = new HashSet<long>(accessionNumbers);

        if (uniqueNumbers.Count == accessionNumbers.Count)
        {
            Console.WriteLine("PASS: All accession numbers are unique.");
        }
        else
        {
            var duplicates = accessionNumbers
                .GroupBy(n => n)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);
            Console.WriteLine($"FAIL: Duplicate accession numbers found: {string.Join(", ", duplicates)}");
        }

        var sorted = accessionNumbers.OrderBy(n => n).ToList();
        bool hasGaps = false;

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] != sorted[i - 1] + 1)
            {
                hasGaps = true;
                break;
            }
        }

        Console.WriteLine(hasGaps
            ? "NOTE: Some gaps exist in the sequence (expected under high concurrency)."
            : "PASS: Accession numbers form a contiguous sequence.");

        Console.WriteLine();
    }

    private static async Task RunDriftFloorRecovery(AtomicCounterOptions options, string counterFile)
    {
        Console.WriteLine("--- Phase 2: Drift-floor recovery (auditor deletes counter file) ---");

        if (File.Exists(counterFile))
        {
            var before = await File.ReadAllTextAsync(counterFile);
            Console.WriteLine($"Counter file before deletion: {before.Trim()}");

            File.Delete(counterFile);
            Console.WriteLine("Counter file deleted by auditor.");
        }

        var scanner = new DriftScanner(options);
        var counter = new FileBasedAtomicCounter(options, scanner);
        var catalog = new LibraryCatalog(counter, options);

        var recoveredCard = await catalog.CatalogItemAsync(
            "Recovery Test Volume",
            "Auditor Scripts",
            "Auditor");

        Console.WriteLine($"Post-recovery allocation: #{recoveredCard.AccessionNumber}");

        var allCards = await catalog.GetAllCardsAsync();
        var allNumbers = allCards.Select(c => c.AccessionNumber).ToList();
        var uniqueAfterRecovery = new HashSet<long>(allNumbers);

        if (uniqueAfterRecovery.Count == allNumbers.Count)
        {
            Console.WriteLine("PASS: No duplicates after drift-floor recovery.");
        }
        else
        {
            Console.WriteLine("FAIL: Duplicates detected after recovery.");
        }

        if (File.Exists(counterFile))
        {
            var after = await File.ReadAllTextAsync(counterFile);
            Console.WriteLine($"Counter file restored to: {after.Trim()}");
        }
    }
}
