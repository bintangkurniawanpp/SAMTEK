namespace SAMTEK.Application.Interfaces;

public record AuthResult(bool Success, string? Username, string? DisplayName, string? Email, string? ErrorMessage);

/// <summary>
/// Pluggable identity backend. Swap MockIdentityProvider for LdapIdentityProvider
/// or IdAManIdentityProvider once the IdAMan API spec is known.
/// </summary>
public interface IIdentityProvider
{
    Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default);
}
