using System.Text.Json;

namespace AtomicCounter.Demo;

/// <summary>
/// Coordinates catalog-card creation by combining the atomic counter for
/// accession-number allocation with file I/O for card persistence.
/// </summary>
public sealed class LibraryCatalog : ILibraryCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly IAtomicCounter _counter;
    private readonly AtomicCounterOptions _options;

    public LibraryCatalog(IAtomicCounter counter, AtomicCounterOptions options)
    {
        _counter = counter;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<CatalogCard> CatalogItemAsync(string title, string author, string librarianName, CancellationToken cancellationToken = default)
    {
        var accessionNumber = await _counter.AllocateAsync(cancellationToken);

        var card = new CatalogCard
        {
            AccessionNumber = accessionNumber,
            Title = title,
            Author = author,
            CatalogedBy = librarianName,
            CatalogedAt = DateTimeOffset.UtcNow,
        };

        Directory.CreateDirectory(_options.ArtifactDirectory);
        var filePath = Path.Combine(_options.ArtifactDirectory, $"card-{accessionNumber}{_options.ArtifactExtension}");
        var json = JsonSerializer.Serialize(card, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);

        return card;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CatalogCard>> GetAllCardsAsync(CancellationToken cancellationToken = default)
    {
        var cards = new List<CatalogCard>();

        if (!Directory.Exists(_options.ArtifactDirectory))
        {
            return cards.AsReadOnly();
        }

        var pattern = $"*{_options.ArtifactExtension}";

        foreach (var file in Directory.EnumerateFiles(_options.ArtifactDirectory, pattern))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var json = await File.ReadAllTextAsync(file, cancellationToken);
            var card = JsonSerializer.Deserialize<CatalogCard>(json, JsonOptions);

            if (card is not null)
            {
                cards.Add(card);
            }
        }

        return cards.OrderBy(c => c.AccessionNumber).ToList().AsReadOnly();
    }
}
