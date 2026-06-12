namespace SAMTEK.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string username, string action, string? detail = null, string? ipAddress = null, CancellationToken ct = default);
}
