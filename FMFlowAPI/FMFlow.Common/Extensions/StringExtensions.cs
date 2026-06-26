using System;

namespace FMFlow.Common.Extensions;

/// <summary>
/// String extension methods for common encoding operations
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// URL-encodes a string for safe use in URLs, query parameters, and form data.
    /// This is equivalent to Uri.EscapeDataString() but provided as a convenient extension method.
    /// </summary>
    /// <param name="value">The string to URL-encode</param>
    /// <returns>URL-encoded string safe for use in URLs</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    /// <example>
    /// <code>
    /// string customerName = "O'Malley & Sons";
    /// string encoded = customerName.ToUrlEncoded(); // "O%27Malley%20%26%20Sons"
    /// 
    /// string searchQuery = "What's the cost?";
    /// string url = $"/api/search?q={searchQuery.ToUrlEncoded()}";
    /// </code>
    /// </example>
    public static string ToUrlEncoded(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Uri.EscapeDataString(value);
    }

    /// <summary>
    /// URL-decodes a string that was previously URL-encoded.
    /// This is equivalent to Uri.UnescapeDataString() but provided as a convenient extension method.
    /// </summary>
    /// <param name="value">The URL-encoded string to decode</param>
    /// <returns>Decoded string</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    /// <example>
    /// <code>
    /// string encoded = "O%27Malley%20%26%20Sons";
    /// string decoded = encoded.FromUrlEncoded(); // "O'Malley & Sons"
    /// </code>
    /// </example>
    public static string FromUrlEncoded(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Uri.UnescapeDataString(value);
    }
}