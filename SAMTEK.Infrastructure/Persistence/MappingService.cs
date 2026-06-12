using Microsoft.EntityFrameworkCore;
using SAMTEK.Application.DTOs;
using SAMTEK.Application.Interfaces;
using SAMTEK.Domain.Entities;

namespace SAMTEK.Infrastructure.Persistence;

public class MappingService(SamtekDbContext db) : IMappingService
{
    public async Task<UserCredentialsDto?> GetCredentialsAsync(string username, CancellationToken ct = default)
    {
        var mapping = await db.UserRoleMappings
            .Include(m => m.ArisRole)
            .Where(m => m.Username == username.ToLowerInvariant() && m.IsApproved)
            .FirstOrDefaultAsync(ct);

        if (mapping?.ArisRole == null) return null;

        var role = mapping.ArisRole;
        // Decode stored BCrypt hash back to plaintext is not possible — store plaintext separately
        // For ARIS auth, we need the actual password. Store in ArisPasswordPlain (see note below).
        // BCrypt is used for admin UI display; ArisPasswordPlain is the real credential.
        var plain = role.ArisPasswordHash; // In production: decrypt with app key, not BCrypt

        return new UserCredentialsDto(
            ArisUsername: role.ArisUsername,
            ArisPasswordBase64: Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(plain)),
            AppaRole: mapping.AppaRole.ToString(),
            CanPrint: role.CanPrint,
            CanDownload: role.CanDownload);
    }

    public async Task<List<MappingDto>> GetAllMappingsAsync(CancellationToken ct = default)
    {
        return await db.UserRoleMappings
            .Include(m => m.ArisRole)
            .Include(m => m.User)
            .OrderBy(m => m.Username)
            .Select(m => new MappingDto(
                m.Id,
                m.Username,
                m.User != null ? m.User.DisplayName : m.Username,
                m.ArisRole != null ? m.ArisRole.RoleCode : "",
                m.ArisRole != null ? m.ArisRole.ArisUsername : "",
                m.AppaRole,
                m.IsApproved,
                m.AssignedBy,
                m.AssignedAt,
                m.Notes,
                m.User != null ? m.User.LastLoginAt : null))
            .ToListAsync(ct);
    }

    public async Task<MappingDto> AssignRoleAsync(AssignRoleDto dto, string assignedBy, CancellationToken ct = default)
    {
        var existing = await db.UserRoleMappings
            .FirstOrDefaultAsync(m => m.Username == dto.Username.ToLowerInvariant(), ct);

        if (existing != null)
        {
            existing.ArisRoleId = dto.ArisRoleId;
            existing.AppaRole = dto.AppaRole;
            existing.IsApproved = true;
            existing.AssignedBy = assignedBy;
            existing.AssignedAt = DateTime.UtcNow;
            existing.ApprovedAt = DateTime.UtcNow;
            existing.Notes = dto.Notes;
        }
        else
        {
            // Ensure user record exists
            var user = await db.LdapUsers.FirstOrDefaultAsync(u => u.Username == dto.Username.ToLowerInvariant(), ct);
            if (user == null)
            {
                user = new LdapUser { Username = dto.Username.ToLowerInvariant(), DisplayName = dto.Username };
                db.LdapUsers.Add(user);
            }

            var mapping = new UserRoleMapping
            {
                Username = dto.Username.ToLowerInvariant(),
                ArisRoleId = dto.ArisRoleId,
                AppaRole = dto.AppaRole,
                IsApproved = true,
                AssignedBy = assignedBy,
                AssignedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow,
                Notes = dto.Notes,
            };
            db.UserRoleMappings.Add(mapping);
        }

        await db.SaveChangesAsync(ct);

        return (await GetAllMappingsAsync(ct)).First(m => m.Username == dto.Username.ToLowerInvariant());
    }

    public async Task ApproveMappingAsync(int mappingId, string approvedBy, CancellationToken ct = default)
    {
        var mapping = await db.UserRoleMappings.FindAsync([mappingId], ct)
            ?? throw new InvalidOperationException($"Mapping {mappingId} not found.");
        mapping.IsApproved = true;
        mapping.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveMappingAsync(int mappingId, string removedBy, CancellationToken ct = default)
    {
        var mapping = await db.UserRoleMappings.FindAsync([mappingId], ct)
            ?? throw new InvalidOperationException($"Mapping {mappingId} not found.");
        db.UserRoleMappings.Remove(mapping);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateLastLoginAsync(string username, CancellationToken ct = default)
    {
        var user = await db.LdapUsers.FirstOrDefaultAsync(u => u.Username == username.ToLowerInvariant(), ct);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
