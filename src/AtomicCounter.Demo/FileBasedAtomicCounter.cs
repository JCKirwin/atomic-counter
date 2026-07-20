namespace AtomicCounter.Demo;

/// <summary>
/// File-lock-guarded monotonic ID allocator. Persists the high-water mark to disk
/// and recovers from corruption or deletion by scanning existing artifacts.
/// </summary>
public sealed class FileBasedAtomicCounter : IAtomicCounter
{
    private readonly AtomicCounterOptions _options;
    private readonly IDriftScanner _driftScanner;

    public FileBasedAtomicCounter(AtomicCounterOptions options, IDriftScanner driftScanner)
    {
        _options = options;
        _driftScanner = driftScanner;
    }

    /// <inheritdoc />
    public async Task<long> AllocateAsync(CancellationToken cancellationToken = default)
    {
        var batch = await AllocateBatchAsync(1, cancellationToken);
        return batch[0];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<long>> AllocateBatchAsync(int count, CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be at least 1.");
        }

        var directory = Path.GetDirectoryName(_options.CounterFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var ids = new List<long>(count);

        using var stream = OpenCounterFileWithLock(_options.CounterFilePath);

        long persisted = ReadPersistedValue(stream);
        long driftFloor = await _driftScanner.ScanForHighestIdAsync(cancellationToken);
        long baseline = Math.Max(persisted, driftFloor);

        for (int i = 1; i <= count; i++)
        {
            ids.Add(baseline + i);
        }

        WritePersistedValue(stream, baseline + count);

        return ids.AsReadOnly();
    }

    private static FileStream OpenCounterFileWithLock(string path)
    {
        const int maxRetries = 50;
        const int retryDelayMs = 20;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                return new FileStream(
                    path,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 256,
                    FileOptions.None);
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                Thread.Sleep(retryDelayMs + Random.Shared.Next(0, retryDelayMs));
            }
        }

        throw new InvalidOperationException($"Could not acquire lock on {path} after {maxRetries} attempts.");
    }

    private static long ReadPersistedValue(FileStream stream)
    {
        if (stream.Length == 0)
        {
            return 0;
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = reader.ReadToEnd().Trim();

        if (long.TryParse(content, out var value) && value >= 0)
        {
            return value;
        }

        return 0;
    }

    private static void WritePersistedValue(FileStream stream, long value)
    {
        stream.Position = 0;
        stream.SetLength(0);
        using var writer = new StreamWriter(stream, leaveOpen: true);
        writer.Write(value.ToString());
        writer.Flush();
    }
}
