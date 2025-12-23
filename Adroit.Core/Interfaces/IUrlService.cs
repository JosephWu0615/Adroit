using Adroit.Core.Entities;

namespace Adroit.Core.Interfaces;

public interface IUrlService
{
    Task<ShortUrl> CreateShortUrlAsync(string longUrl, string? customShortCode = null);
    Task<string?> GetLongUrlAsync(string shortCode);
    Task<ShortUrl?> GetUrlDetailsAsync(string shortCode);
    Task<bool> DeleteShortUrlAsync(string shortCode);
    Task<IEnumerable<ShortUrl>> GetAllUrlsAsync();
    Task<IEnumerable<ShortUrl>> GetUrlsByLongUrlAsync(string longUrl);
    Task RecordClickAsync(string shortCode);
}
