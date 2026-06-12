namespace SAMTEK.Web.Services;

/// <summary>
/// Scoped per-circuit session state for Blazor Server.
/// Holds admin login state and the APPA token returned after regular user login.
/// </summary>
public class SamtekSession
{
    public bool IsAdminAuthenticated { get; set; }
    public string? AdminUsername { get; set; }

    public void ClearAdmin()
    {
        IsAdminAuthenticated = false;
        AdminUsername = null;
    }
}
