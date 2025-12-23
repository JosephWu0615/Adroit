namespace Adroit.Core.Exceptions;

public class UrlNotFoundException : Exception
{
    public string ShortCode { get; }

    public UrlNotFoundException(string shortCode)
        : base($"URL with short code '{shortCode}' was not found.")
    {
        ShortCode = shortCode;
    }

    public UrlNotFoundException(string shortCode, string message)
        : base(message)
    {
        ShortCode = shortCode;
    }
}
