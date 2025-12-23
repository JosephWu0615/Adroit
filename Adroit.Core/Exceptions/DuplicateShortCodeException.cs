namespace Adroit.Core.Exceptions;

public class DuplicateShortCodeException : Exception
{
    public string ShortCode { get; }

    public DuplicateShortCodeException(string shortCode)
        : base($"Short code '{shortCode}' already exists.")
    {
        ShortCode = shortCode;
    }

    public DuplicateShortCodeException(string shortCode, string message)
        : base(message)
    {
        ShortCode = shortCode;
    }
}
