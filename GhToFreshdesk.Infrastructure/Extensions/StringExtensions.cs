namespace GhToFreshdesk.Infrastructure.Extensions;

public static class StringExtensions
{
    /// <summary>
    /// Returns the original string if shorter than or equal to maxLength,
    /// otherwise returns a truncated substring.
    /// </summary>
    public static string? Truncate(this string? value, int maxLength = 4000)
    {
        if (value is null) return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}