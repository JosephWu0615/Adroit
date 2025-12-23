using Microsoft.AspNetCore.Mvc;
using Adroit.Core.Interfaces;
using Adroit.Core.Exceptions;
using Adroit.API.Models;
using Adroit.API.Models.DTOs;

namespace Adroit.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UrlsController : ControllerBase
{
    private readonly IUrlService _urlService;
    private readonly ILogger<UrlsController> _logger;
    private readonly string _baseUrl;

    public UrlsController(
        IUrlService urlService,
        ILogger<UrlsController> logger,
        IConfiguration configuration)
    {
        _urlService = urlService;
        _logger = logger;
        _baseUrl = configuration["AppSettings:BaseUrl"] ?? $"{Request?.Scheme}://{Request?.Host}" ?? "http://localhost:5000";
    }

    /// <summary>
    /// Create a new short URL
    /// </summary>
    /// <param name="request">The URL creation request</param>
    /// <returns>The created short URL details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<UrlResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateShortUrl([FromBody] CreateUrlRequest request)
    {
        try
        {
            var shortUrl = await _urlService.CreateShortUrlAsync(
                request.LongUrl,
                request.CustomShortCode);

            var baseUrl = GetBaseUrl();
            var response = new UrlResponse
            {
                Id = shortUrl.Id,
                ShortCode = shortUrl.ShortCode,
                ShortUrl = $"{baseUrl}/{shortUrl.ShortCode}",
                LongUrl = shortUrl.LongUrl,
                ClickCount = shortUrl.ClickCount,
                CreatedAt = shortUrl.CreatedAt
            };

            return CreatedAtAction(
                nameof(GetUrlDetails),
                new { shortCode = shortUrl.ShortCode },
                ApiResponse<UrlResponse>.Ok(response, "Short URL created successfully"));
        }
        catch (InvalidUrlException ex)
        {
            _logger.LogWarning(ex, "Invalid URL provided: {Url}", request.LongUrl);
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (InvalidShortCodeException ex)
        {
            _logger.LogWarning(ex, "Invalid short code provided: {ShortCode}", request.CustomShortCode);
            return BadRequest(ApiResponse.Fail(ex.Message));
        }
        catch (DuplicateShortCodeException ex)
        {
            _logger.LogWarning(ex, "Duplicate short code: {ShortCode}", request.CustomShortCode);
            return Conflict(ApiResponse.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get all short URLs
    /// </summary>
    /// <returns>List of all short URLs</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UrlResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllUrls()
    {
        var urls = await _urlService.GetAllUrlsAsync();
        var baseUrl = GetBaseUrl();

        var response = urls.Select(u => new UrlResponse
        {
            Id = u.Id,
            ShortCode = u.ShortCode,
            ShortUrl = $"{baseUrl}/{u.ShortCode}",
            LongUrl = u.LongUrl,
            ClickCount = u.ClickCount,
            CreatedAt = u.CreatedAt,
            LastAccessedAt = u.LastAccessedAt
        });

        return Ok(ApiResponse<IEnumerable<UrlResponse>>.Ok(response));
    }

    /// <summary>
    /// Get URL details by short code
    /// </summary>
    /// <param name="shortCode">The short code to look up</param>
    /// <returns>The URL details</returns>
    [HttpGet("{shortCode}")]
    [ProducesResponseType(typeof(ApiResponse<UrlResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlDetails(string shortCode)
    {
        var shortUrl = await _urlService.GetUrlDetailsAsync(shortCode);

        if (shortUrl == null)
        {
            return NotFound(ApiResponse.Fail($"Short URL '{shortCode}' not found"));
        }

        var baseUrl = GetBaseUrl();
        var response = new UrlResponse
        {
            Id = shortUrl.Id,
            ShortCode = shortUrl.ShortCode,
            ShortUrl = $"{baseUrl}/{shortUrl.ShortCode}",
            LongUrl = shortUrl.LongUrl,
            ClickCount = shortUrl.ClickCount,
            CreatedAt = shortUrl.CreatedAt,
            LastAccessedAt = shortUrl.LastAccessedAt
        };

        return Ok(ApiResponse<UrlResponse>.Ok(response));
    }

    /// <summary>
    /// Delete a short URL
    /// </summary>
    /// <param name="shortCode">The short code to delete</param>
    [HttpDelete("{shortCode}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteShortUrl(string shortCode)
    {
        var deleted = await _urlService.DeleteShortUrlAsync(shortCode);

        if (!deleted)
        {
            return NotFound(ApiResponse.Fail($"Short URL '{shortCode}' not found"));
        }

        return NoContent();
    }

    /// <summary>
    /// Get click statistics for a short URL
    /// </summary>
    /// <param name="shortCode">The short code to get stats for</param>
    /// <returns>The URL statistics</returns>
    [HttpGet("{shortCode}/stats")]
    [ProducesResponseType(typeof(ApiResponse<UrlStatsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrlStats(string shortCode)
    {
        var shortUrl = await _urlService.GetUrlDetailsAsync(shortCode);

        if (shortUrl == null)
        {
            return NotFound(ApiResponse.Fail($"Short URL '{shortCode}' not found"));
        }

        var daysSinceCreation = (int)(DateTime.UtcNow - shortUrl.CreatedAt).TotalDays;
        var avgClicksPerDay = daysSinceCreation > 0
            ? (double)shortUrl.ClickCount / daysSinceCreation
            : shortUrl.ClickCount;

        var baseUrl = GetBaseUrl();
        var response = new UrlStatsResponse
        {
            ShortCode = shortUrl.ShortCode,
            ShortUrl = $"{baseUrl}/{shortUrl.ShortCode}",
            LongUrl = shortUrl.LongUrl,
            ClickCount = shortUrl.ClickCount,
            CreatedAt = shortUrl.CreatedAt,
            LastAccessedAt = shortUrl.LastAccessedAt,
            AverageClicksPerDay = Math.Round(avgClicksPerDay, 2),
            DaysSinceCreation = daysSinceCreation
        };

        return Ok(ApiResponse<UrlStatsResponse>.Ok(response));
    }

    /// <summary>
    /// Find all short URLs for a given long URL
    /// </summary>
    /// <param name="longUrl">The long URL to search for</param>
    /// <returns>List of short URLs pointing to the long URL</returns>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UrlResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> LookupByLongUrl([FromQuery] string longUrl)
    {
        if (string.IsNullOrWhiteSpace(longUrl))
        {
            return BadRequest(ApiResponse.Fail("Long URL is required"));
        }

        var urls = await _urlService.GetUrlsByLongUrlAsync(longUrl);
        var baseUrl = GetBaseUrl();

        var response = urls.Select(u => new UrlResponse
        {
            Id = u.Id,
            ShortCode = u.ShortCode,
            ShortUrl = $"{baseUrl}/{u.ShortCode}",
            LongUrl = u.LongUrl,
            ClickCount = u.ClickCount,
            CreatedAt = u.CreatedAt,
            LastAccessedAt = u.LastAccessedAt
        });

        return Ok(ApiResponse<IEnumerable<UrlResponse>>.Ok(response));
    }

    private string GetBaseUrl()
    {
        // Try to get base URL from config, fallback to request URL
        var configBaseUrl = HttpContext.RequestServices
            .GetService<IConfiguration>()?["AppSettings:BaseUrl"];

        if (!string.IsNullOrEmpty(configBaseUrl))
        {
            return configBaseUrl.TrimEnd('/');
        }

        return $"{Request.Scheme}://{Request.Host}";
    }
}
