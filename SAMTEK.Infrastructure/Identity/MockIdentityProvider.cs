using Microsoft.Extensions.Configuration;
using SAMTEK.Application.Interfaces;

namespace SAMTEK.Infrastructure.Identity;

/// <summary>
/// Testing identity provider. Validates against a static list in appsettings MockUsers[].
/// If no MockUsers configured, falls back to accepting any username with password "password".
/// </summary>
public class MockIdentityProvider(IConfiguration config) : IIdentityProvider
{
    public Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return Task.FromResult(Fail("Username dan password wajib diisi."));

        var staticUsers = config.GetSection("MockUsers").Get<List<MockUser>>() ?? [];

        if (staticUsers.Count > 0)
        {
            var match = staticUsers.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

            if (match == null)
                return Task.FromResult(Fail("Username atau password salah."));

            return Task.FromResult(new AuthResult(
                Success: true,
                Username: match.Username.ToLowerInvariant(),
                DisplayName: match.DisplayName ?? match.Username,
                Email: $"{match.Username.ToLowerInvariant()}@example.com",
                ErrorMessage: null));
        }

        // Fallback: accept any user with password "password"
        if (password != "password")
            return Task.FromResult(Fail("Username atau password salah."));

        return Task.FromResult(new AuthResult(
            Success: true,
            Username: username.ToLowerInvariant(),
            DisplayName: username,
            Email: $"{username.ToLowerInvariant()}@example.com",
            ErrorMessage: null));
    }

    private static AuthResult Fail(string msg) => new(false, null, null, null, msg);
}

public class MockUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? DisplayName { get; set; }
}
