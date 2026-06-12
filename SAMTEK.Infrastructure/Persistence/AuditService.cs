using SAMTEK.Application.Interfaces;
using SAMTEK.Domain.Entities;

namespace SAMTEK.Infrastructure.Persistence;

public class AuditService(SamtekDbContext db) : IAuditService
{
    public async Task LogAsync(string username, string action, string? detail = null, string? ipAddress = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Username = username,
            Action = action,
            Detail = detail,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
