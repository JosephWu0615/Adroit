using System.Collections.Concurrent;
using Adroit.Core.Entities;
using Adroit.Core.Interfaces;

namespace Adroit.Data.Repositories;

public class InMemoryUrlRepository : IUrlRepository
{
    // Thread-safe primary storage by short code (normalized to lowercase)
    private readonly ConcurrentDictionary<string, ShortUrl> _urlsByShortCode = new();

    // Secondary index by ID for faster lookups
    private readonly ConcurrentDictionary<Guid, ShortUrl> _urlsById = new();

    // Index for long URL lookups (one-to-many: long URL -> multiple short codes)
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _shortCodesByLongUrl = new();

    // Lock for complex operations that need atomicity
    private readonly object _indexLock = new();

    public Task<ShortUrl?> GetByShortCodeAsync(string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return Task.FromResult<ShortUrl?>(null);

        _urlsByShortCode.TryGetValue(NormalizeCode(shortCode), out var shortUrl);
        return Task.FromResult(shortUrl);
    }

    public Task<ShortUrl?> GetByIdAsync(Guid id)
    {
        _urlsById.TryGetValue(id, out var shortUrl);
        return Task.FromResult(shortUrl);
    }

    public Task<IEnumerable<ShortUrl>> GetByLongUrlAsync(string longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
            return Task.FromResult(Enumerable.Empty<ShortUrl>());

        if (_shortCodesByLongUrl.TryGetValue(longUrl, out var shortCodes))
        {
            var urls = shortCodes
                .Select(code => _urlsByShortCode.TryGetValue(code, out var url) ? url : null)
                .Where(url => url != null)
                .Cast<ShortUrl>()
                .ToList();
            return Task.FromResult<IEnumerable<ShortUrl>>(urls);
        }

        return Task.FromResult(Enumerable.Empty<ShortUrl>());
    }

    public Task<IEnumerable<ShortUrl>> GetAllAsync()
    {
        return Task.FromResult<IEnumerable<ShortUrl>>(_urlsByShortCode.Values.ToList());
    }

    public Task<ShortUrl> AddAsync(ShortUrl shortUrl)
    {
        if (shortUrl == null)
            throw new ArgumentNullException(nameof(shortUrl));

        var normalizedCode = NormalizeCode(shortUrl.ShortCode);

        // Try to add to primary storage - this is atomic
        if (!_urlsByShortCode.TryAdd(normalizedCode, shortUrl))
        {
            throw new InvalidOperationException($"Short code '{shortUrl.ShortCode}' already exists");
        }

        // Add to secondary indexes
        _urlsById.TryAdd(shortUrl.Id, shortUrl);

        // Add to long URL index (thread-safe)
        _shortCodesByLongUrl.AddOrUpdate(
            shortUrl.LongUrl,
            _ => new ConcurrentBag<string> { normalizedCode },
            (_, bag) => { bag.Add(normalizedCode); return bag; }
        );

        return Task.FromResult(shortUrl);
    }

    public Task<bool> DeleteAsync(string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return Task.FromResult(false);

        var normalizedCode = NormalizeCode(shortCode);

        if (_urlsByShortCode.TryRemove(normalizedCode, out var removed))
        {
            // Remove from ID index
            _urlsById.TryRemove(removed.Id, out _);

            // Remove from long URL index
            lock (_indexLock)
            {
                if (_shortCodesByLongUrl.TryGetValue(removed.LongUrl, out var bag))
                {
                    // ConcurrentBag doesn't support removal, so we rebuild it
                    var remaining = bag.Where(c => c != normalizedCode).ToList();
                    if (remaining.Count == 0)
                    {
                        _shortCodesByLongUrl.TryRemove(removed.LongUrl, out _);
                    }
                    else
                    {
                        _shortCodesByLongUrl[removed.LongUrl] = new ConcurrentBag<string>(remaining);
                    }
                }
            }

            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    public Task<bool> ExistsAsync(string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return Task.FromResult(false);

        return Task.FromResult(_urlsByShortCode.ContainsKey(NormalizeCode(shortCode)));
    }

    public Task<ShortUrl> UpdateAsync(ShortUrl shortUrl)
    {
        if (shortUrl == null)
            throw new ArgumentNullException(nameof(shortUrl));

        var normalizedCode = NormalizeCode(shortUrl.ShortCode);
        shortUrl.UpdatedAt = DateTime.UtcNow;

        _urlsByShortCode[normalizedCode] = shortUrl;
        _urlsById[shortUrl.Id] = shortUrl;

        return Task.FromResult(shortUrl);
    }

    public Task<long> GetTotalCountAsync()
    {
        return Task.FromResult((long)_urlsByShortCode.Count);
    }

    private static string NormalizeCode(string code)
    {
        return code.ToLowerInvariant();
    }
}
