using SAMTEK.Domain.Enums;

namespace SAMTEK.Domain.Entities;

/// <summary>
/// Maps an IdAMan/LDAP user to a shared ARIS license account and an APPA role.
/// One user has at most one active mapping.
/// </summary>
public class UserRoleMapping
{
    public int Id { get; set; }
    public string Username { get; set; } = "";          // IdAMan username (FK to LdapUser.Username)
    public int ArisRoleId { get; set; }
    public AppRole AppaRole { get; set; }
    public bool IsApproved { get; set; }
    public string AssignedBy { get; set; } = "";        // admin username
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }

    public LdapUser? User { get; set; }
    public ArisRole? ArisRole { get; set; }
}
