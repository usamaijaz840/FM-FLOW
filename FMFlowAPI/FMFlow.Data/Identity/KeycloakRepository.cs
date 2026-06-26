using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Text.Json;
using FMFlow.Identity;
using FMFlow.Identity.Interface;
using FMFlow.Identity.Interface.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FMFlow.Data.Identity;

public class KeycloakRepository : IIdentityRepository
{
	private readonly HttpClient keycloakHttpClient;
	private readonly ILogger<KeycloakRepository> _logger;
	public KeycloakConfiguration KeycloakConfiguration { get; }

	private static readonly JsonSerializerOptions JsonOpts = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public KeycloakRepository(IHttpClientFactory factory,
							  IOptions<KeycloakConfiguration> cfg,
							  ILogger<KeycloakRepository> logger)
	{
		keycloakHttpClient = factory.CreateClient("keycloak");
		_logger = logger;
		KeycloakConfiguration = cfg.Value;

		if (keycloakHttpClient.BaseAddress is null)
		{
			throw new ApplicationException("Keycloak HttpClient BaseAddress not set");
		}
	}

	public async Task<TokenResponseDto?> AuthenticateUser(string emailAddress, string password, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(emailAddress))
		{
			throw new ArgumentException("Email address is required", nameof(emailAddress));
		}

		if (string.IsNullOrWhiteSpace(password))
		{
			throw new ArgumentException("Password is required", nameof(password));
		}

		var tokenPath = $"realms/{KeycloakConfiguration.KeycloakRealm}/protocol/openid-connect/token";

		var values = new Dictionary<string, string>
		{
			["grant_type"] = "password",
			["client_id"] = KeycloakConfiguration.ClientId,
			["username"] = emailAddress,
			["password"] = password,
			["scope"] = "openid profile email"
		};

		if (!string.IsNullOrEmpty(KeycloakConfiguration.ClientSecret))
		{
			values["client_secret"] = KeycloakConfiguration.ClientSecret;
		}

		using var content = new FormUrlEncodedContent(values);

		using var response = await keycloakHttpClient.PostAsync(tokenPath, content, ct);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogError("Failed to authenticate user {Email}. StatusCode={Status}", emailAddress, response.StatusCode);
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return JsonSerializer.Deserialize<TokenResponseDto>(json, JsonOpts);
	}

	public async Task<TokenResponseDto?> RefreshAccessToken(string refreshToken, CancellationToken ct)
	{
		var tokenPath = $"realms/{KeycloakConfiguration.KeycloakRealm}/protocol/openid-connect/token";

		var values = new Dictionary<string, string>
		{
			["grant_type"] = "refresh_token",
			["client_id"] = KeycloakConfiguration.ClientId,
			["refresh_token"] = refreshToken
		};

		if (!string.IsNullOrEmpty(KeycloakConfiguration.ClientSecret))
		{
			values["client_secret"] = KeycloakConfiguration.ClientSecret;
		}

		using var content = new FormUrlEncodedContent(values);
		using var response = await keycloakHttpClient.PostAsync(tokenPath, content, ct);
		if (!response.IsSuccessStatusCode) return null;

		var json = await response.Content.ReadAsStringAsync(ct);
		return JsonSerializer.Deserialize<TokenResponseDto>(json, JsonOpts);
	}

	public string? GetEmailFromAccessToken(string accessToken)
	{
		var handler = new JwtSecurityTokenHandler();
		if (!handler.CanReadToken(accessToken)) return null;

		var jwt = handler.ReadJwtToken(accessToken);
		return jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
	}

	public async Task<Guid?> CreateUser(string email, string password, string firstName, string lastName, int? externalId, int? leadId, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var payload = new
		{
			email,
			username = email,
			firstName,
			lastName,
			attributes = new { external_id = externalId, lead_id = leadId },
			enabled = true,
			credentials = new[] { new { type = "password", value = password, temporary = false } }
		};

		using var response = await keycloakHttpClient.PostAsJsonAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users", payload, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to create user: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
		}

		if (response.Headers.Location is not null)
		{
			var id = response.Headers.Location.ToString().Split('/').Last();
			return Guid.Parse(id);
		}
		return null;
	}

	public async Task<List<KeycloakUser>> GetUsersByRole(Roles role, CancellationToken ct, int? first = null, int? max = null)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var qp = new List<string>();
		if (first.HasValue) qp.Add($"first={first.Value}");
		if (max.HasValue) qp.Add($"max={max.Value}");
		var suffix = qp.Count > 0 ? "?" + string.Join("&", qp) : string.Empty;

		using var response = await keycloakHttpClient.GetAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/roles/{role.ToString().ToLower()}/users{suffix}", ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to obtain users by role: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return JsonSerializer.Deserialize<List<KeycloakUser>>(json, JsonOpts) ?? new();
	}

	public async Task<KeycloakRole?> GetRoleByName(Roles role, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		using var response = await keycloakHttpClient.GetAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/roles/{role.ToString().ToLower()}", ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to obtain role: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
		}

		return await response.Content.ReadFromJsonAsync<KeycloakRole>(cancellationToken: ct);
	}

	public async Task<bool> DoesUserIdExist(string emailAddress, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		using var response = await keycloakHttpClient.GetAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users?email={Uri.EscapeDataString(emailAddress)}", ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to check user existence: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
		}

		var users = await response.Content.ReadFromJsonAsync<User[]>(cancellationToken: ct);
		return users is { Length: > 0 };
	}

	public async Task AssignRoleToUser(Guid userId, KeycloakRole role, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var payload = new[] { new { id = role.Id, name = role.Name } };
		using var response = await keycloakHttpClient.PostAsJsonAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users/{userId}/role-mappings/realm", payload, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to assign role: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
		}
	}

	public async Task RemoveRoleFromUser(Guid userId, KeycloakRole role, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var payload = new[] { new { id = role.Id, name = role.Name } };
		using var request = new HttpRequestMessage(HttpMethod.Delete, $"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users/{userId}/role-mappings/realm")
		{
			Content = JsonContent.Create(payload)
		};

		using var response = await keycloakHttpClient.SendAsync(request, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to remove role: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
		}
	}

	public async Task<string> GetAdminTokenAsync(CancellationToken ct)
	{
		var tokenPath = $"realms/{KeycloakConfiguration.KeycloakRealm}/protocol/openid-connect/token";

		var values = new Dictionary<string, string>
		{
			["grant_type"] = "client_credentials",
			["client_id"] = KeycloakConfiguration.ClientId,
			["client_secret"] = KeycloakConfiguration.ClientSecret
		};

		using var content = new FormUrlEncodedContent(values);
		using var response = await keycloakHttpClient.PostAsync(tokenPath, content, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to obtain admin token: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		var parsed = JsonSerializer.Deserialize<TokenResponseDto>(json, JsonOpts);

		if (parsed == null || string.IsNullOrWhiteSpace(parsed.AccessToken))
		{
			throw new Exception("Failed to extract access token from response.");
		}

		return parsed.AccessToken;
	}

	public async Task<List<KeycloakUser>?> SearchUsers(string searchTerm, int pageIndex, int pageSize, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var payload = new { search = searchTerm, page = pageIndex, max = pageSize };
		using var response = await keycloakHttpClient.PostAsJsonAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users", payload, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to get users: {await response.Content.ReadAsStringAsync(ct)}");
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return JsonSerializer.Deserialize<List<KeycloakUser>>(json, JsonOpts);
	}

	public async Task<List<Roles>> GetUserRoles(Guid identityGuid, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var url = $"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users/{identityGuid}/role-mappings/realm";
		using var response = await keycloakHttpClient.GetAsync(url, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to get users: {await response.Content.ReadAsStringAsync()}");
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		var roles = Newtonsoft.Json.JsonConvert.DeserializeObject<List<KeycloakRole>>(json) ?? new List<KeycloakRole>();

		return roles
			.Where(r => Enum.TryParse(typeof(Roles), r.Name, true, out _))
			.Select(r => (Roles)Enum.Parse(typeof(Roles), r.Name, true))
			.ToList();
	}

	public async Task SetUserPassword(string userId, string newPassword, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var payload = new { type = "password", value = newPassword, temporary = false };
		using var response = await keycloakHttpClient.PutAsJsonAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users/{userId}/reset-password", payload, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to reset password: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
		}
	}

	public async Task UpdateUserEmail(string userId, string newEmail, CancellationToken ct)
	{
		var token = await GetAdminTokenAsync(ct);
		keycloakHttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

		var payload = new { email = newEmail, username = newEmail };
		using var response = await keycloakHttpClient.PutAsJsonAsync($"admin/realms/{KeycloakConfiguration.KeycloakRealm}/users/{userId}", payload, ct);

		if (!response.IsSuccessStatusCode)
		{
			throw new Exception($"Failed to update email: {response.StatusCode} - {await response.Content.ReadAsStringAsync(ct)}");
		}
	}

	public async Task<TokenResponseDto?> AuthenticateServiceClient(string clientId, string clientSecret, CancellationToken ct)
	{
		var tokenPath = $"realms/{KeycloakConfiguration.KeycloakRealm}/protocol/openid-connect/token";

		var values = new Dictionary<string, string>
		{
			["grant_type"] = "client_credentials",
			["client_id"] = clientId,
			["client_secret"] = clientSecret
		};

		using var content = new FormUrlEncodedContent(values);
		using var response = await keycloakHttpClient.PostAsync(tokenPath, content, ct);

		if (!response.IsSuccessStatusCode)
		{
			_logger.LogWarning("Service client auth failed for client_id {ClientId}. StatusCode={StatusCode}", clientId, response.StatusCode);
			return null;
		}

		var json = await response.Content.ReadAsStringAsync(ct);
		return JsonSerializer.Deserialize<TokenResponseDto>(json, JsonOpts);
	}

	private class User
	{
		public string Id { get; set; } = string.Empty;
		public string Username { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
	}
}
