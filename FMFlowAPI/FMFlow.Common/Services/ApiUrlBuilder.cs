using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using FMFlow.Common;

namespace FMFlow.Common.Services;

/// <summary>
/// Implementation of IApiUrlBuilder that builds URLs based on configuration.
/// </summary>
public class ApiUrlBuilder : IApiUrlBuilder
{
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly AppSettings _appSettings;

	public ApiUrlBuilder(IHttpContextAccessor httpContextAccessor, IOptions<AppSettings> appSettings)
	{
		_httpContextAccessor = httpContextAccessor;
		_appSettings = appSettings.Value;
	}

	/// <inheritdoc/>
	public string GetBaseUrl()
	{
		// Use configured base URL if available
		if (!string.IsNullOrEmpty(_appSettings.BaseApiUrl))
		{
			return _appSettings.BaseApiUrl.TrimEnd('/');
		}

		// Fallback to request-based URL
		// FUTURE: There may be a way to always be able to get the base URL this way without needing to specify in configuration
		var request = _httpContextAccessor.HttpContext?.Request 
			?? throw new InvalidOperationException("HttpContext is not available and App:BaseApiUrl is not configured");

		return $"{request.Scheme}://{request.Host}";
	}

	/// <inheritdoc/>
	public string GetFullUrl(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
			throw new ArgumentException("Path cannot be null or empty", nameof(path));

		if (!path.StartsWith('/'))
			path = $"/{path}";

		return $"{GetBaseUrl()}{path}";
	}
}
