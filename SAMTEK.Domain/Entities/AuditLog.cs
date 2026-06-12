namespace SAMTEK.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Action { get; set; } = "";    // LOGIN_SUCCESS | LOGIN_FAIL | LOGOUT | ROLE_ASSIGNED | ROLE_REMOVED | LOCKED
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
