namespace Adroit.API.Models.DTOs;

public record UrlResponse
{
    public Guid Id { get; init; }
    public string ShortCode { get; init; } = string.Empty;
    public string ShortUrl { get; init; } = string.Empty;
    public string LongUrl { get; init; } = string.Empty;
    public long ClickCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastAccessedAt { get; init; }
}
