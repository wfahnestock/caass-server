namespace CAASS.Auth.Utils;

public static class StringExtensions
{
    public static string RemoveWhitespace(this string input)
    {
        return new string(input
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
    }
}