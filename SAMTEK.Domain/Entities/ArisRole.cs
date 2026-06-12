namespace SAMTEK.Domain.Entities;

/// <summary>
/// Shared ARIS license account. Multiple IdAMan users share one ARIS credential
/// to bypass the per-seat license limit.
/// </summary>
public class ArisRole
{
    public int Id { get; set; }
    public string RoleCode { get; set; } = "";          // e.g. "DESIGNER_1", "VIEWER_1"
    public string ArisUsername { get; set; } = "";      // shared ARIS account username
    public string ArisPasswordHash { get; set; } = "";  // BCrypt hash (never stored plain)
    public string RoleType { get; set; } = "";          // "Designer" | "Viewer"
    public bool CanPrint { get; set; }
    public bool CanDownload { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<UserRoleMapping> UserMappings { get; set; } = [];
}
