using Adroit.Core.Entities;
using Adroit.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Adroit.Data.Repositories;

public class SqlUrlRepository : IUrlRepository
{
    private readonly AdroitDbContext _context;

    public SqlUrlRepository(AdroitDbContext context)
    {
        _context = context;
    }

    public async Task<ShortUrl?> GetByShortCodeAsync(string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return null;

        return await _context.ShortUrls
            .FirstOrDefaultAsync(u => u.ShortCode.ToLower() == shortCode.ToLower());
    }

    public async Task<ShortUrl?> GetByIdAsync(Guid id)
    {
        return await _context.ShortUrls.FindAsync(id);
    }

    public async Task<IEnumerable<ShortUrl>> GetByLongUrlAsync(string longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
            return Enumerable.Empty<ShortUrl>();

        return await _context.ShortUrls
            .Where(u => u.LongUrl == longUrl)
            .ToListAsync();
    }

    public async Task<IEnumerable<ShortUrl>> GetAllAsync()
    {
        return await _context.ShortUrls
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<ShortUrl> AddAsync(ShortUrl shortUrl)
    {
        _context.ShortUrls.Add(shortUrl);
        await _context.SaveChangesAsync();
        return shortUrl;
    }

    public async Task<bool> DeleteAsync(string shortCode)
    {
        var entity = await GetByShortCodeAsync(shortCode);
        if (entity == null)
            return false;

        _context.ShortUrls.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string shortCode)
    {
        if (string.IsNullOrWhiteSpace(shortCode))
            return false;

        return await _context.ShortUrls
            .AnyAsync(u => u.ShortCode.ToLower() == shortCode.ToLower());
    }

    public async Task<ShortUrl> UpdateAsync(ShortUrl shortUrl)
    {
        shortUrl.UpdatedAt = DateTime.UtcNow;
        _context.ShortUrls.Update(shortUrl);
        await _context.SaveChangesAsync();
        return shortUrl;
    }

    public async Task<long> GetTotalCountAsync()
    {
        return await _context.ShortUrls.LongCountAsync();
    }
}
