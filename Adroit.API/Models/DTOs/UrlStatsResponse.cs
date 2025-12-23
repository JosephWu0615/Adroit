namespace Adroit.API.Models.DTOs;

public record UrlStatsResponse
{
    public string ShortCode { get; init; } = string.Empty;
    public string ShortUrl { get; init; } = string.Empty;
    public string LongUrl { get; init; } = string.Empty;
    public long ClickCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastAccessedAt { get; init; }
    public double AverageClicksPerDay { get; init; }
    public int DaysSinceCreation { get; init; }
}
