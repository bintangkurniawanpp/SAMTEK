using Microsoft.EntityFrameworkCore;
using SAMTEK.Application.Interfaces;
using SAMTEK.Domain.Entities;
using SAMTEK.Infrastructure.Persistence;

namespace SAMTEK.Infrastructure.Identity;

public class AdminAuthService(SamtekDbContext db) : IAdminAuthService
{
    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken ct = default)
    {
        var admin = await db.AdminUsers
            .FirstOrDefaultAsync(a => a.Username == username.ToLowerInvariant() && a.IsActive, ct);

        if (admin == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash);
    }

    public Task<bool> HasAnyAdminAsync(CancellationToken ct = default)
        => db.AdminUsers.AnyAsync(a => a.IsActive, ct);

    public async Task CreateAdminAsync(string username, string password, CancellationToken ct = default)
    {
        db.AdminUsers.Add(new AdminUser
        {
            Username = username.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
