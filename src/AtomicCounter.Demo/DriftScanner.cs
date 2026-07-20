using System.Text.RegularExpressions;

namespace AtomicCounter.Demo;

/// <summary>
/// Scans a directory of catalog-card JSON files to find the highest accession number.
/// </summary>
public sealed partial class DriftScanner : IDriftScanner
{
    private readonly AtomicCounterOptions _options;

    public DriftScanner(AtomicCounterOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public Task<long> ScanForHighestIdAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_options.ArtifactDirectory))
        {
            return Task.FromResult(0L);
        }

        long highest = 0;
        var pattern = $"*{_options.ArtifactExtension}";

        foreach (var file in Directory.EnumerateFiles(_options.ArtifactDirectory, pattern))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileName = Path.GetFileNameWithoutExtension(file);
            var match = AccessionNumberPattern().Match(fileName);

            if (match.Success && long.TryParse(match.Groups[1].Value, out var id))
            {
                if (id > highest)
                {
                    highest = id;
                }
            }
        }

        return Task.FromResult(highest);
    }

    [GeneratedRegex(@"(\d+)$")]
    private static partial Regex AccessionNumberPattern();
}
