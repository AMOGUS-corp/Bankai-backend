using System.Text.RegularExpressions;

namespace Bankai.MLApi.Infrastructure.Extensions;

public static class StringExtensions
{
    private static readonly Regex Regex = new Regex("\\w+");

    public static bool IsEmpty(this string value) => string.IsNullOrEmpty(value);

    public static bool IsWhiteSpaces(this string value) => string.IsNullOrWhiteSpace(value);

    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrEmpty(value);

    public static bool IsNullOrWhiteSpaces(this string? value) => string.IsNullOrWhiteSpace(value);

    public static string Trim(this string value, string trimString)
    {
        return value.TrimStart(trimString).TrimEnd(trimString);
    }

    public static string TrimStart(this string value, string trimString)
    {
        if (trimString.IsNullOrWhiteSpaces() || !value.StartsWith(trimString))
            return value;
        string str = value;
        int length = trimString.Length;
        return str.Substring(length, str.Length - length).TrimStart(trimString);
    }

    public static string TrimEnd(this string value, string trimString)
    {
        if (trimString.IsNullOrWhiteSpaces() || !value.EndsWith(trimString))
            return value;
        string str = value;
        int length = trimString.Length;
        return str.Substring(0, str.Length - length).TrimEnd(trimString);
    }

    public static IEnumerable<string> Words(this string value)
    {
        return StringExtensions.Regex.Matches(value).Select<Match, string>((Func<Match, string>) (x => x.Value));
    }
}