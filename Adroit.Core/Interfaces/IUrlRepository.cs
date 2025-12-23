using Adroit.Core.Entities;

namespace Adroit.Core.Interfaces;

public interface IUrlRepository
{
    Task<ShortUrl?> GetByShortCodeAsync(string shortCode);
    Task<ShortUrl?> GetByIdAsync(Guid id);
    Task<IEnumerable<ShortUrl>> GetByLongUrlAsync(string longUrl);
    Task<IEnumerable<ShortUrl>> GetAllAsync();
    Task<ShortUrl> AddAsync(ShortUrl shortUrl);
    Task<bool> DeleteAsync(string shortCode);
    Task<bool> ExistsAsync(string shortCode);
    Task<ShortUrl> UpdateAsync(ShortUrl shortUrl);
    Task<long> GetTotalCountAsync();
}
