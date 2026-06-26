using System.Text.Json;

namespace FMFlow.IntegrationTest;

/// <summary>
/// HTTP response assertion helpers for integration tests.
/// </summary>
internal static class HttpResponseMessageTestExtensions
{
	/// <summary>
	/// Ensures the response status code is successful (2xx). If not, reads and includes the full body in the thrown exception.
	/// Attempts to pretty-print JSON bodies (including ProblemDetails). Intended for test diagnostics.
	/// </summary>
	/// <exception cref="HttpRequestException">Always thrown when the response is non-success.</exception>
	public static async Task EnsureSuccessStatusCodeWithContent(this HttpResponseMessage response, CancellationToken cancellationToken = default)
	{
		if (response.IsSuccessStatusCode)
		{
			return;
		}

		string body;

		try
		{
			body = await ReadPossiblyPrettyJsonAsync(response.Content, cancellationToken);
		}
		catch
		{
			body = await response.Content.ReadAsStringAsync(cancellationToken);
		}

		var statusLine = $"{(int)response.StatusCode} {response.ReasonPhrase}";
		var contentType = response.Content.Headers.ContentType?.MediaType ?? "(none)";

		var message =
			$"HTTP request failed: {statusLine}\nContent-Type: {contentType}\nBody:\n{body}";

		throw new HttpRequestException(message, null, response.StatusCode);
	}

	/// <summary>
	/// Reads the raw response body and pretty-prints if JSON; otherwise returns raw content.
	/// </summary>
	private static async Task<string> ReadPossiblyPrettyJsonAsync(HttpContent content, CancellationToken ct)
	{
		var mediaType = content.Headers.ContentType?.MediaType;
		var raw = await content.ReadAsStringAsync(ct);

		if (string.IsNullOrWhiteSpace(raw) ||
			mediaType is null ||
			!mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
		{
			return raw;
		}

		try
		{
			using var doc = JsonDocument.Parse(raw);
			return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = true });
		}
		catch
		{
			return raw;
		}
	}
}
