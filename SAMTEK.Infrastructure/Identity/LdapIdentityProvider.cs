using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.Extensions.Options;
using SAMTEK.Application.Interfaces;

namespace SAMTEK.Infrastructure.Identity;

public class LdapSettings
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 389;
    public string BaseDn { get; set; } = "";         // e.g. "DC=pertamina,DC=com"
    public string UserSearchFilter { get; set; } = "(sAMAccountName={0})";
    public string ServiceAccountDn { get; set; } = "";
    public string ServiceAccountPassword { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// Active Directory / LDAP identity provider.
/// Configure via appsettings: Ldap:Host, Ldap:BaseDn, etc.
/// </summary>
public class LdapIdentityProvider(IOptions<LdapSettings> opts) : IIdentityProvider
{
    private readonly LdapSettings _cfg = opts.Value;

    public async Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        return await Task.Run(() => Authenticate(username, password), ct);
    }

    private AuthResult Authenticate(string username, string password)
    {
        try
        {
            using var conn = new LdapConnection(new LdapDirectoryIdentifier(_cfg.Host, _cfg.Port))
            {
                AuthType = AuthType.Basic,
                Timeout = TimeSpan.FromSeconds(_cfg.TimeoutSeconds),
            };
            conn.SessionOptions.ProtocolVersion = 3;

            // Step 1: bind with service account to search for user DN
            conn.Bind(new NetworkCredential(_cfg.ServiceAccountDn, _cfg.ServiceAccountPassword));

            var filter = string.Format(_cfg.UserSearchFilter, username);
            var req = new SearchRequest(_cfg.BaseDn, filter, SearchScope.Subtree,
                "distinguishedName", "displayName", "mail", "sAMAccountName");
            var resp = (SearchResponse)conn.SendRequest(req);

            if (resp.Entries.Count == 0)
                return new AuthResult(false, null, null, null, "User not found in directory.");

            var entry = resp.Entries[0];
            var userDn = entry.DistinguishedName;
            var displayName = entry.Attributes["displayName"]?[0]?.ToString() ?? username;
            var email = entry.Attributes["mail"]?[0]?.ToString() ?? "";

            // Step 2: bind as the user to validate their password
            conn.Bind(new NetworkCredential(userDn, password));

            return new AuthResult(true, username.ToLowerInvariant(), displayName, email, null);
        }
        catch (LdapException ex) when (ex.ErrorCode == 49)
        {
            return new AuthResult(false, null, null, null, "Invalid username or password.");
        }
        catch (Exception ex)
        {
            return new AuthResult(false, null, null, null, $"LDAP error: {ex.Message}");
        }
    }
}
