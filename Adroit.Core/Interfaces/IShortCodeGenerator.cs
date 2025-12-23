namespace Adroit.Core.Interfaces;

public interface IShortCodeGenerator
{
    string Generate(int length = 7);
    bool IsValidShortCode(string code);
}
