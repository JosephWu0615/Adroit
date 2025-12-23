namespace Adroit.Core.Exceptions;

public class InvalidShortCodeException : Exception
{
    public string ShortCode { get; }

    public InvalidShortCodeException(string shortCode)
        : base($"Invalid short code format: '{shortCode}'. Short codes must be 4-12 alphanumeric characters.")
    {
        ShortCode = shortCode;
    }

    public InvalidShortCodeException(string shortCode, string message)
        : base(message)
    {
        ShortCode = shortCode;
    }
}
