using System.ComponentModel.DataAnnotations;

namespace Adroit.API.Models.DTOs;

public record CreateUrlRequest
{
    [Required(ErrorMessage = "Long URL is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string LongUrl { get; init; } = string.Empty;

    /// <summary>
    /// Optional custom short code. If not provided, one will be auto-generated.
    /// Must be 4-12 alphanumeric characters.
    /// </summary>
    public string? CustomShortCode { get; init; }
}
