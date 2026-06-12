namespace SAMTEK.Domain.Entities;

public class LdapUser
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsLocked { get; set; }
    public int FailedAttempts { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    public UserRoleMapping? RoleMapping { get; set; }
}
