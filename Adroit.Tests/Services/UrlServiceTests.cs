using Adroit.Core.Entities;
using Adroit.Core.Exceptions;
using Adroit.Core.Interfaces;
using Adroit.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Adroit.Tests.Services;

public class UrlServiceTests
{
    private readonly Mock<IUrlRepository> _mockRepository;
    private readonly Mock<IShortCodeGenerator> _mockGenerator;
    private readonly Mock<ILogger<UrlService>> _mockLogger;
    private readonly IConfiguration _configuration;
    private readonly UrlService _service;

    public UrlServiceTests()
    {
        _mockRepository = new Mock<IUrlRepository>();
        _mockGenerator = new Mock<IShortCodeGenerator>();
        _mockLogger = new Mock<ILogger<UrlService>>();

        var configData = new Dictionary<string, string?>
        {
            { "AppSettings:BaseUrl", "http://localhost:5000" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new UrlService(
            _mockRepository.Object,
            _mockGenerator.Object,
            _mockLogger.Object,
            _configuration);
    }

    [Fact]
    public async Task CreateShortUrlAsync_ShouldGenerateCode_WhenNoCustomCodeProvided()
    {
        // Arrange
        var longUrl = "https://example.com";
        var generatedCode = "abc1234";

        _mockGenerator.Setup(g => g.Generate(It.IsAny<int>())).Returns(generatedCode);
        _mockRepository.Setup(r => r.ExistsAsync(generatedCode)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShortUrl>()))
            .ReturnsAsync((ShortUrl s) => s);

        // Act
        var result = await _service.CreateShortUrlAsync(longUrl);

        // Assert
        Assert.Equal(generatedCode, result.ShortCode);
        Assert.Equal(longUrl, result.LongUrl);
        _mockGenerator.Verify(g => g.Generate(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task CreateShortUrlAsync_ShouldUseCustomCode_WhenProvided()
    {
        // Arrange
        var longUrl = "https://example.com";
        var customCode = "mycode";

        _mockGenerator.Setup(g => g.IsValidShortCode(customCode)).Returns(true);
        _mockRepository.Setup(r => r.ExistsAsync(customCode)).ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShortUrl>()))
            .ReturnsAsync((ShortUrl s) => s);

        // Act
        var result = await _service.CreateShortUrlAsync(longUrl, customCode);

        // Assert
        Assert.Equal(customCode, result.ShortCode);
        _mockGenerator.Verify(g => g.Generate(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateShortUrlAsync_ShouldThrowInvalidUrlException_ForInvalidUrl()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";

        // Act & Assert
        await Assert.ThrowsAsync<InvalidUrlException>(
            () => _service.CreateShortUrlAsync(invalidUrl));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ftp://example.com")]
    [InlineData("javascript:alert(1)")]
    public async Task CreateShortUrlAsync_ShouldThrowInvalidUrlException_ForNonHttpUrls(string invalidUrl)
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidUrlException>(
            () => _service.CreateShortUrlAsync(invalidUrl));
    }

    [Fact]
    public async Task CreateShortUrlAsync_ShouldThrowDuplicateShortCodeException_WhenCustomCodeExists()
    {
        // Arrange
        var longUrl = "https://example.com";
        var customCode = "existing";

        _mockGenerator.Setup(g => g.IsValidShortCode(customCode)).Returns(true);
        _mockRepository.Setup(r => r.ExistsAsync(customCode)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateShortCodeException>(
            () => _service.CreateShortUrlAsync(longUrl, customCode));
    }

    [Fact]
    public async Task CreateShortUrlAsync_ShouldThrowInvalidShortCodeException_WhenCustomCodeInvalid()
    {
        // Arrange
        var longUrl = "https://example.com";
        var invalidCode = "ab"; // Too short

        _mockGenerator.Setup(g => g.IsValidShortCode(invalidCode)).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidShortCodeException>(
            () => _service.CreateShortUrlAsync(longUrl, invalidCode));
    }

    [Fact]
    public async Task CreateShortUrlAsync_ShouldRetryOnCollision()
    {
        // Arrange
        var longUrl = "https://example.com";
        var callCount = 0;

        _mockGenerator.Setup(g => g.Generate(It.IsAny<int>()))
            .Returns(() => callCount++ < 2 ? "collision" : "unique");

        _mockRepository.Setup(r => r.ExistsAsync("collision")).ReturnsAsync(true);
        _mockRepository.Setup(r => r.ExistsAsync("unique")).ReturnsAsync(false);
        _mockRepository.Setup(r => r.AddAsync(It.IsAny<ShortUrl>()))
            .ReturnsAsync((ShortUrl s) => s);

        // Act
        var result = await _service.CreateShortUrlAsync(longUrl);

        // Assert
        Assert.Equal("unique", result.ShortCode);
        _mockGenerator.Verify(g => g.Generate(It.IsAny<int>()), Times.Exactly(3));
    }

    [Fact]
    public async Task GetLongUrlAsync_ShouldReturnLongUrl_WhenExists()
    {
        // Arrange
        var shortUrl = new ShortUrl("abc1234", "https://example.com");
        _mockRepository.Setup(r => r.GetByShortCodeAsync("abc1234")).ReturnsAsync(shortUrl);

        // Act
        var result = await _service.GetLongUrlAsync("abc1234");

        // Assert
        Assert.Equal("https://example.com", result);
    }

    [Fact]
    public async Task GetLongUrlAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByShortCodeAsync("nonexistent")).ReturnsAsync((ShortUrl?)null);

        // Act
        var result = await _service.GetLongUrlAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteShortUrlAsync_ShouldReturnTrue_WhenDeleted()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync("abc1234")).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteShortUrlAsync("abc1234");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteShortUrlAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync("nonexistent")).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteShortUrlAsync("nonexistent");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RecordClickAsync_ShouldIncrementClickCount()
    {
        // Arrange
        var shortUrl = new ShortUrl("abc1234", "https://example.com");
        _mockRepository.Setup(r => r.GetByShortCodeAsync("abc1234")).ReturnsAsync(shortUrl);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<ShortUrl>()))
            .ReturnsAsync((ShortUrl s) => s);

        // Act
        await _service.RecordClickAsync("abc1234");

        // Assert
        Assert.Equal(1, shortUrl.ClickCount);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ShortUrl>()), Times.Once);
    }

    [Fact]
    public async Task RecordClickAsync_ShouldDoNothing_WhenUrlNotFound()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByShortCodeAsync("nonexistent")).ReturnsAsync((ShortUrl?)null);

        // Act
        await _service.RecordClickAsync("nonexistent");

        // Assert
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<ShortUrl>()), Times.Never);
    }

    [Fact]
    public async Task GetAllUrlsAsync_ShouldReturnAllUrls()
    {
        // Arrange
        var urls = new List<ShortUrl>
        {
            new("code1", "https://example1.com"),
            new("code2", "https://example2.com")
        };
        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(urls);

        // Act
        var result = await _service.GetAllUrlsAsync();

        // Assert
        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetUrlsByLongUrlAsync_ShouldReturnMatchingUrls()
    {
        // Arrange
        var longUrl = "https://example.com";
        var urls = new List<ShortUrl>
        {
            new("code1", longUrl),
            new("code2", longUrl)
        };
        _mockRepository.Setup(r => r.GetByLongUrlAsync(longUrl)).ReturnsAsync(urls);

        // Act
        var result = await _service.GetUrlsByLongUrlAsync(longUrl);

        // Assert
        Assert.Equal(2, result.Count());
    }
}
