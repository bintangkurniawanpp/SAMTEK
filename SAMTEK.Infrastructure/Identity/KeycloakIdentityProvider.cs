using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SAMTEK.Application.Interfaces;

namespace SAMTEK.Infrastructure.Identity;

public class KeycloakSettings
{
    public string BaseUrl { get; set; } = "http://localhost:8080";
    public string Realm { get; set; } = "samtek";
    public string ClientId { get; set; } = "samtek-app";
    public string ClientSecret { get; set; } = "";
}

/// <summary>
/// Authenticates users against Keycloak using the Resource Owner Password Credentials grant.
/// Requires "Direct access grants" enabled on the Keycloak client.
/// </summary>
public class KeycloakIdentityProvider(
    IOptions<KeycloakSettings> opts,
    IHttpClientFactory httpClientFactory) : IIdentityProvider
{
    private readonly KeycloakSettings _cfg = opts.Value;

    public async Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var tokenUrl = $"{_cfg.BaseUrl}/realms/{_cfg.Realm}/protocol/openid-connect/token";

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "password",
            ["client_id"]     = _cfg.ClientId,
            ["client_secret"] = _cfg.ClientSecret,
            ["username"]      = username,
            ["password"]      = password,
            ["scope"]         = "openid profile email",
        };

        try
        {
            var client = httpClientFactory.CreateClient("keycloak");
            var resp = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form), ct);

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(ct);
                var msg = resp.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "Username atau password salah."
                    : $"Keycloak error ({(int)resp.StatusCode}): {err}";
                return new AuthResult(false, null, null, null, msg);
            }

            var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
            var accessToken = json.GetProperty("access_token").GetString()!;

            // Decode JWT payload (middle segment) — no signature verification needed,
            // Keycloak already validated credentials before issuing this token.
            var payload = DecodeJwtPayload(accessToken);

            var displayName = GetClaim(payload, "name")
                           ?? GetClaim(payload, "preferred_username")
                           ?? username;
            var email = GetClaim(payload, "email") ?? "";
            var preferredUsername = GetClaim(payload, "preferred_username") ?? username;

            return new AuthResult(true, preferredUsername.ToLowerInvariant(), displayName, email, null);
        }
        catch (HttpRequestException ex)
        {
            return new AuthResult(false, null, null, null, $"Tidak dapat terhubung ke Keycloak: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, null, null, $"Keycloak error: {ex.Message}");
        }
    }

    private static JsonElement DecodeJwtPayload(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return default;
        var padded = parts[1].PadRight(parts[1].Length + (4 - parts[1].Length % 4) % 4, '=');
        var bytes = Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        return JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(bytes));
    }

    private static string? GetClaim(JsonElement payload, string key)
    {
        if (payload.ValueKind == JsonValueKind.Object &&
            payload.TryGetProperty(key, out var val) &&
            val.ValueKind == JsonValueKind.String)
            return val.GetString();
        return null;
    }
}
