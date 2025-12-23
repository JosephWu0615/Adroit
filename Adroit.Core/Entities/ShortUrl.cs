namespace Adroit.Core.Entities;

public class ShortUrl : BaseEntity
{
    public string ShortCode { get; set; } = string.Empty;
    public string LongUrl { get; set; } = string.Empty;

    // EF Core compatible property with backing field for thread-safe operations
    private long _clickCount;
    public long ClickCount
    {
        get => _clickCount;
        set => _clickCount = value; // Setter for EF Core
    }

    public DateTime? LastAccessedAt { get; set; }

    public ShortUrl()
    {
        _clickCount = 0;
    }

    public ShortUrl(string shortCode, string longUrl) : this()
    {
        ShortCode = shortCode;
        LongUrl = longUrl;
    }

    /// <summary>
    /// Thread-safe increment of click count
    /// </summary>
    public void IncrementClickCount()
    {
        Interlocked.Increment(ref _clickCount);
        LastAccessedAt = DateTime.UtcNow;
    }
}
