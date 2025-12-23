using System.Security.Cryptography;
using System.Text;
using Adroit.Core.Interfaces;

namespace Adroit.Infrastructure.Utilities;

public class ShortCodeGenerator : IShortCodeGenerator
{
    // Base62 character set: a-z, A-Z, 0-9 (URL-safe characters)
    private const string Base62Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public const int DefaultLength = 7;
    public const int MinLength = 4;
    public const int MaxLength = 12;

    /// <summary>
    /// Generates a cryptographically random short code using Base62 encoding.
    /// </summary>
    /// <param name="length">The length of the short code (default: 7)</param>
    /// <returns>A random alphanumeric short code</returns>
    public string Generate(int length = DefaultLength)
    {
        if (length < MinLength || length > MaxLength)
        {
            throw new ArgumentException($"Length must be between {MinLength} and {MaxLength}", nameof(length));
        }

        var sb = new StringBuilder(length);
        var randomBytes = new byte[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        foreach (var b in randomBytes)
        {
            // Use modulo to map byte to Base62 character
            sb.Append(Base62Chars[b % Base62Chars.Length]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates whether a short code meets the required format.
    /// </summary>
    /// <param name="code">The short code to validate</param>
    /// <returns>True if the code is valid, false otherwise</returns>
    public bool IsValidShortCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return false;

        if (code.Length < MinLength || code.Length > MaxLength)
            return false;

        // Check that all characters are alphanumeric (Base62)
        return code.All(c => Base62Chars.Contains(c));
    }
}
