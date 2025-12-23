using Microsoft.AspNetCore.Mvc;
using Adroit.Core.Interfaces;

namespace Adroit.API.Controllers;

[ApiController]
public class RedirectController : ControllerBase
{
    private readonly IUrlService _urlService;
    private readonly ILogger<RedirectController> _logger;

    public RedirectController(IUrlService urlService, ILogger<RedirectController> logger)
    {
        _urlService = urlService;
        _logger = logger;
    }

    /// <summary>
    /// Redirect a short URL to its long URL destination
    /// </summary>
    /// <param name="shortCode">The short code to resolve</param>
    /// <returns>Redirect to the long URL or 404 if not found</returns>
    [HttpGet("/{shortCode}")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ApiExplorerSettings(IgnoreApi = true)] // Hide from Swagger since it's not a typical API endpoint
    public async Task<IActionResult> RedirectToLongUrl(string shortCode)
    {
        // Skip if it looks like an API call, static file, or reserved path
        if (IsReservedPath(shortCode))
        {
            return NotFound();
        }

        var longUrl = await _urlService.GetLongUrlAsync(shortCode);

        if (longUrl == null)
        {
            _logger.LogWarning("Short URL not found: {ShortCode}", shortCode);
            return NotFound(new { error = $"Short URL '{shortCode}' not found" });
        }

        // Record the click asynchronously (fire and forget for performance)
        // The click is recorded even if the redirect fails on the client side
        _ = Task.Run(async () =>
        {
            try
            {
                await _urlService.RecordClickAsync(shortCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to record click for {ShortCode}", shortCode);
            }
        });

        _logger.LogInformation("Redirecting {ShortCode} to {LongUrl}", shortCode, longUrl);
        return Redirect(longUrl);
    }

    private static bool IsReservedPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return true;

        // Reserved paths that should not be treated as short codes
        var reservedPaths = new[]
        {
            "api",
            "swagger",
            "health",
            "favicon.ico",
            "robots.txt",
            "_framework"
        };

        var lowerPath = path.ToLowerInvariant();

        // Check if path starts with reserved prefix
        if (reservedPaths.Any(p => lowerPath.StartsWith(p)))
            return true;

        // Check if path contains a file extension (likely static file)
        if (path.Contains('.'))
            return true;

        return false;
    }
}
