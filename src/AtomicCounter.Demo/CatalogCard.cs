namespace AtomicCounter.Demo;

/// <summary>
/// A library catalog card representing a single accessioned item.
/// Written as a JSON file to the shared artifact directory.
/// </summary>
public sealed record CatalogCard
{
    /// <summary>
    /// The unique, monotonically assigned accession number.
    /// </summary>
    public required long AccessionNumber { get; init; }

    /// <summary>
    /// Title of the cataloged item.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Author of the cataloged item.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Name of the librarian who cataloged this item.
    /// </summary>
    public required string CatalogedBy { get; init; }

    /// <summary>
    /// Timestamp when the item was cataloged.
    /// </summary>
    public DateTimeOffset CatalogedAt { get; init; } = DateTimeOffset.UtcNow;
}
