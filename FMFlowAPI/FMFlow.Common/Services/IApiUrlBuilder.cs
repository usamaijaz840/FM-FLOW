namespace FMFlow.Common.Services;

/// <summary>
/// Provides methods for building URLs based on the current HTTP request context.
/// </summary>
public interface IApiUrlBuilder
{
	/// <summary>
	/// Gets the base URL (scheme and host) of the current request.
	/// </summary>
	/// <returns>The base URL in the format: {scheme}://{host}</returns>
	string GetBaseUrl();

	/// <summary>
	/// Gets the full URL for a given path based on the current request.
	/// </summary>
	/// <param name="path">The path to append to the base URL. Will be prefixed with '/' if not already present.</param>
	/// <returns>The full URL in the format: {scheme}://{host}{path}</returns>
	string GetFullUrl(string path);
}
