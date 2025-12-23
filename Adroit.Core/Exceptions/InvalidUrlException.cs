namespace Adroit.Core.Exceptions;

public class InvalidUrlException : Exception
{
    public string Url { get; }

    public InvalidUrlException(string url)
        : base($"Invalid URL format: '{url}'")
    {
        Url = url;
    }

    public InvalidUrlException(string url, string message)
        : base(message)
    {
        Url = url;
    }
}
