using Adroit.Infrastructure.Utilities;
using Xunit;

namespace Adroit.Tests.Utilities;

public class ShortCodeGeneratorTests
{
    private readonly ShortCodeGenerator _generator;

    public ShortCodeGeneratorTests()
    {
        _generator = new ShortCodeGenerator();
    }

    [Fact]
    public void Generate_ShouldReturnCodeWithDefaultLength()
    {
        // Act
        var code = _generator.Generate();

        // Assert
        Assert.Equal(ShortCodeGenerator.DefaultLength, code.Length);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(7)]
    [InlineData(12)]
    public void Generate_ShouldReturnCodeWithSpecifiedLength(int length)
    {
        // Act
        var code = _generator.Generate(length);

        // Assert
        Assert.Equal(length, code.Length);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(13)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Generate_ShouldThrowForInvalidLength(int length)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _generator.Generate(length));
    }

    [Fact]
    public void Generate_ShouldOnlyContainAlphanumericCharacters()
    {
        // Arrange
        const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        // Act
        var code = _generator.Generate();

        // Assert
        Assert.All(code, c => Assert.Contains(c, validChars));
    }

    [Fact]
    public void Generate_ShouldProduceUniqueValues()
    {
        // Arrange
        const int count = 1000;
        var codes = new HashSet<string>();

        // Act
        for (int i = 0; i < count; i++)
        {
            codes.Add(_generator.Generate());
        }

        // Assert - All generated codes should be unique
        Assert.Equal(count, codes.Count);
    }

    [Theory]
    [InlineData("abc1234", true)]      // Valid 7-char code
    [InlineData("ABCD", true)]          // Valid 4-char uppercase
    [InlineData("abcd", true)]          // Valid 4-char lowercase
    [InlineData("1234567890AB", true)]  // Valid 12-char code
    [InlineData("ab", false)]           // Too short (3 chars min)
    [InlineData("abc", false)]          // Too short (4 chars min)
    [InlineData("abcdefghijklm", false)] // Too long (13 chars)
    [InlineData("abc-123", false)]      // Invalid character (hyphen)
    [InlineData("abc_123", false)]      // Invalid character (underscore)
    [InlineData("abc 123", false)]      // Invalid character (space)
    [InlineData("", false)]             // Empty string
    [InlineData(null, false)]           // Null
    public void IsValidShortCode_ShouldValidateCorrectly(string? code, bool expected)
    {
        // Act
        var result = _generator.IsValidShortCode(code!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Generate_ShouldBeThreadSafe()
    {
        // Arrange
        const int threadCount = 10;
        const int iterationsPerThread = 100;
        var codes = new System.Collections.Concurrent.ConcurrentBag<string>();
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < threadCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerThread; j++)
                {
                    codes.Add(_generator.Generate());
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(threadCount * iterationsPerThread, codes.Count);
        Assert.All(codes, code => Assert.Equal(ShortCodeGenerator.DefaultLength, code.Length));
    }
}
