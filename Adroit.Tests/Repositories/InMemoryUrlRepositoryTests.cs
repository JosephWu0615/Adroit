using Adroit.Core.Entities;
using Adroit.Data.Repositories;
using Xunit;

namespace Adroit.Tests.Repositories;

public class InMemoryUrlRepositoryTests
{
    private readonly InMemoryUrlRepository _repository;

    public InMemoryUrlRepositoryTests()
    {
        _repository = new InMemoryUrlRepository();
    }

    [Fact]
    public async Task AddAsync_ShouldAddUrl_WhenShortCodeIsUnique()
    {
        // Arrange
        var shortUrl = new ShortUrl("abc1234", "https://example.com");

        // Act
        var result = await _repository.AddAsync(shortUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("abc1234", result.ShortCode);
        Assert.Equal("https://example.com", result.LongUrl);
    }

    [Fact]
    public async Task AddAsync_ShouldThrow_WhenShortCodeAlreadyExists()
    {
        // Arrange
        var shortUrl1 = new ShortUrl("abc1234", "https://example1.com");
        var shortUrl2 = new ShortUrl("abc1234", "https://example2.com");
        await _repository.AddAsync(shortUrl1);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddAsync(shortUrl2));
    }

    [Fact]
    public async Task AddAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var shortUrl1 = new ShortUrl("ABC1234", "https://example1.com");
        var shortUrl2 = new ShortUrl("abc1234", "https://example2.com");
        await _repository.AddAsync(shortUrl1);

        // Act & Assert - should throw because ABC1234 and abc1234 are the same
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddAsync(shortUrl2));
    }

    [Fact]
    public async Task GetByShortCodeAsync_ShouldReturnUrl_WhenExists()
    {
        // Arrange
        var shortUrl = new ShortUrl("abc1234", "https://example.com");
        await _repository.AddAsync(shortUrl);

        // Act
        var result = await _repository.GetByShortCodeAsync("abc1234");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("abc1234", result.ShortCode);
    }

    [Fact]
    public async Task GetByShortCodeAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByShortCodeAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByShortCodeAsync_ShouldBeCaseInsensitive()
    {
        // Arrange
        var shortUrl = new ShortUrl("ABC1234", "https://example.com");
        await _repository.AddAsync(shortUrl);

        // Act
        var result = await _repository.GetByShortCodeAsync("abc1234");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("ABC1234", result.ShortCode);
    }

    [Fact]
    public async Task GetByLongUrlAsync_ShouldReturnMultipleShortCodes()
    {
        // Arrange
        var longUrl = "https://example.com";
        await _repository.AddAsync(new ShortUrl("code1", longUrl));
        await _repository.AddAsync(new ShortUrl("code2", longUrl));
        await _repository.AddAsync(new ShortUrl("code3", longUrl));

        // Act
        var results = await _repository.GetByLongUrlAsync(longUrl);

        // Assert
        Assert.Equal(3, results.Count());
    }

    [Fact]
    public async Task GetByLongUrlAsync_ShouldReturnEmpty_WhenNoMatches()
    {
        // Act
        var results = await _repository.GetByLongUrlAsync("https://nonexistent.com");

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUrl_WhenExists()
    {
        // Arrange
        var shortUrl = new ShortUrl("abc1234", "https://example.com");
        await _repository.AddAsync(shortUrl);

        // Act
        var deleted = await _repository.DeleteAsync("abc1234");

        // Assert
        Assert.True(deleted);
        Assert.False(await _repository.ExistsAsync("abc1234"));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var deleted = await _repository.DeleteAsync("nonexistent");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldUpdateLongUrlIndex()
    {
        // Arrange
        var longUrl = "https://example.com";
        await _repository.AddAsync(new ShortUrl("code1", longUrl));
        await _repository.AddAsync(new ShortUrl("code2", longUrl));

        // Act
        await _repository.DeleteAsync("code1");
        var results = await _repository.GetByLongUrlAsync(longUrl);

        // Assert
        Assert.Single(results);
        Assert.Equal("code2", results.First().ShortCode);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        await _repository.AddAsync(new ShortUrl("abc1234", "https://example.com"));

        // Act
        var exists = await _repository.ExistsAsync("abc1234");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Act
        var exists = await _repository.ExistsAsync("nonexistent");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllUrls()
    {
        // Arrange
        await _repository.AddAsync(new ShortUrl("code1", "https://example1.com"));
        await _repository.AddAsync(new ShortUrl("code2", "https://example2.com"));
        await _repository.AddAsync(new ShortUrl("code3", "https://example3.com"));

        // Act
        var results = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(3, results.Count());
    }

    [Fact]
    public async Task GetTotalCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _repository.AddAsync(new ShortUrl("code1", "https://example1.com"));
        await _repository.AddAsync(new ShortUrl("code2", "https://example2.com"));

        // Act
        var count = await _repository.GetTotalCountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateExistingUrl()
    {
        // Arrange
        var shortUrl = new ShortUrl("abc1234", "https://example.com");
        await _repository.AddAsync(shortUrl);

        // Simulate a click
        shortUrl.IncrementClickCount();

        // Act
        await _repository.UpdateAsync(shortUrl);
        var result = await _repository.GetByShortCodeAsync("abc1234");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ClickCount);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task Repository_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int operationsPerThread = 50;
        var tasks = new List<Task>();

        // Act - Multiple threads adding URLs concurrently
        for (int i = 0; i < threadCount; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < operationsPerThread; j++)
                {
                    var code = $"code{threadId}_{j}";
                    await _repository.AddAsync(new ShortUrl(code, $"https://example{threadId}_{j}.com"));
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var count = await _repository.GetTotalCountAsync();
        Assert.Equal(threadCount * operationsPerThread, count);
    }
}
