using Adroit.Core.Entities;
using Adroit.Core.Exceptions;
using Adroit.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Adroit.Infrastructure.Services;

public class UrlService : IUrlService
{
    private readonly IUrlRepository _repository;
    private readonly IShortCodeGenerator _codeGenerator;
    private readonly ILogger<UrlService> _logger;
    private readonly string _baseUrl;
    private const int MaxRetries = 5;

    public UrlService(
        IUrlRepository repository,
        IShortCodeGenerator codeGenerator,
        ILogger<UrlService> logger,
        IConfiguration configuration)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseUrl = configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
    }

    public async Task<ShortUrl> CreateShortUrlAsync(string longUrl, string? customShortCode = null)
    {
        // Validate URL format
        if (!IsValidUrl(longUrl))
        {
            throw new InvalidUrlException(longUrl);
        }

        string shortCode;

        if (!string.IsNullOrWhiteSpace(customShortCode))
        {
            // Custom short code provided - validate and check uniqueness
            if (!_codeGenerator.IsValidShortCode(customShortCode))
            {
                throw new InvalidShortCodeException(customShortCode);
            }

            if (await _repository.ExistsAsync(customShortCode))
            {
                throw new DuplicateShortCodeException(customShortCode);
            }

            shortCode = customShortCode;
            _logger.LogInformation("Using custom short code: {ShortCode}", shortCode);
        }
        else
        {
            // Generate unique short code with retry logic
            shortCode = await GenerateUniqueShortCodeAsync();
            _logger.LogInformation("Generated short code: {ShortCode}", shortCode);
        }

        var shortUrl = new ShortUrl(shortCode, longUrl);
        await _repository.AddAsync(shortUrl);

        _logger.LogInformation("Created short URL: {ShortCode} -> {LongUrl}", shortCode, longUrl);
        return shortUrl;
    }

    public async Task<string?> GetLongUrlAsync(string shortCode)
    {
        var shortUrl = await _repository.GetByShortCodeAsync(shortCode);
        return shortUrl?.LongUrl;
    }

    public async Task<ShortUrl?> GetUrlDetailsAsync(string shortCode)
    {
        return await _repository.GetByShortCodeAsync(shortCode);
    }

    public async Task<bool> DeleteShortUrlAsync(string shortCode)
    {
        var result = await _repository.DeleteAsync(shortCode);

        if (result)
        {
            _logger.LogInformation("Deleted short URL: {ShortCode}", shortCode);
        }
        else
        {
            _logger.LogWarning("Failed to delete short URL (not found): {ShortCode}", shortCode);
        }

        return result;
    }

    public async Task<IEnumerable<ShortUrl>> GetAllUrlsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<ShortUrl>> GetUrlsByLongUrlAsync(string longUrl)
    {
        return await _repository.GetByLongUrlAsync(longUrl);
    }

    public async Task RecordClickAsync(string shortCode)
    {
        var shortUrl = await _repository.GetByShortCodeAsync(shortCode);
        if (shortUrl != null)
        {
            shortUrl.IncrementClickCount();
            await _repository.UpdateAsync(shortUrl);
            _logger.LogDebug("Recorded click for: {ShortCode}, total clicks: {ClickCount}",
                shortCode, shortUrl.ClickCount);
        }
    }

    private async Task<string> GenerateUniqueShortCodeAsync()
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            var code = _codeGenerator.Generate();

            if (!await _repository.ExistsAsync(code))
            {
                return code;
            }

            _logger.LogDebug("Short code collision detected: {Code}, retrying ({Attempt}/{MaxRetries})",
                code, i + 1, MaxRetries);
        }

        throw new InvalidOperationException(
            $"Failed to generate unique short code after {MaxRetries} attempts. This may indicate the namespace is nearly exhausted.");
    }

    private static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        // Only allow HTTP and HTTPS schemes
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
