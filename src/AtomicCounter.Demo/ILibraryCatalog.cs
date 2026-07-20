namespace AtomicCounter.Demo;

/// <summary>
/// Service that manages the library catalog: assigns accession numbers and
/// persists catalog cards to the shared artifact directory.
/// </summary>
public interface ILibraryCatalog
{
    /// <summary>
    /// Catalogs a new item, allocating a unique accession number and writing the card to disk.
    /// </summary>
    Task<CatalogCard> CatalogItemAsync(string title, string author, string librarianName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all catalog cards from the artifact directory.
    /// </summary>
    Task<IReadOnlyList<CatalogCard>> GetAllCardsAsync(CancellationToken cancellationToken = default);
}
