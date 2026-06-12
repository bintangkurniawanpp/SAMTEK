using Microsoft.EntityFrameworkCore;
using SAMTEK.Domain.Entities;

namespace SAMTEK.Infrastructure.Persistence;

public class SamtekDbContext(DbContextOptions<SamtekDbContext> options) : DbContext(options)
{
    public DbSet<LdapUser> LdapUsers => Set<LdapUser>();
    public DbSet<ArisRole> ArisRoles => Set<ArisRole>();
    public DbSet<UserRoleMapping> UserRoleMappings => Set<UserRoleMapping>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<LdapUser>(e =>
        {
            e.ToTable("ldap_users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Email).HasMaxLength(200);
        });

        b.Entity<ArisRole>(e =>
        {
            e.ToTable("aris_roles");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.RoleCode).IsUnique();
            e.Property(x => x.RoleCode).HasMaxLength(50).IsRequired();
            e.Property(x => x.ArisUsername).HasMaxLength(100).IsRequired();
            e.Property(x => x.ArisPasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.RoleType).HasMaxLength(20).IsRequired();
        });

        b.Entity<UserRoleMapping>(e =>
        {
            e.ToTable("user_role_mappings");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.AssignedBy).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(500);
            e.HasOne(x => x.User).WithOne(x => x.RoleMapping)
                .HasForeignKey<UserRoleMapping>(x => x.Username)
                .HasPrincipalKey<LdapUser>(x => x.Username)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ArisRole).WithMany(x => x.UserMappings)
                .HasForeignKey(x => x.ArisRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.Action).HasMaxLength(50).IsRequired();
            e.Property(x => x.Detail).HasMaxLength(1000);
            e.Property(x => x.IpAddress).HasMaxLength(50);
        });

        b.Entity<AdminUser>(e =>
        {
            e.ToTable("admin_users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.Property(x => x.PasswordHash).HasMaxLength(200).IsRequired();
        });
    }
}
