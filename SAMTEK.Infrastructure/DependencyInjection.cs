using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SAMTEK.Application.Interfaces;
using SAMTEK.Infrastructure.Identity;
using SAMTEK.Infrastructure.Persistence;

namespace SAMTEK.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<SamtekDbContext>(opts =>
            opts.UseNpgsql(config.GetConnectionString("DefaultConnection")));

        services.AddScoped<IMappingService, MappingService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IAdminAuthService, AdminAuthService>();

        // Identity provider: set IdentityProvider:Type to "Ldap", "Keycloak", or "Mock"
        var providerType = config["IdentityProvider:Type"] ?? "Mock";
        if (providerType == "Ldap")
        {
            services.Configure<LdapSettings>(o =>
            {
                o.Host = config["Ldap:Host"] ?? "";
                o.Port = int.TryParse(config["Ldap:Port"], out var p) ? p : 389;
                o.BaseDn = config["Ldap:BaseDn"] ?? "";
                o.UserSearchFilter = config["Ldap:UserSearchFilter"] ?? "(sAMAccountName={0})";
                o.ServiceAccountDn = config["Ldap:ServiceAccountDn"] ?? "";
                o.ServiceAccountPassword = config["Ldap:ServiceAccountPassword"] ?? "";
                o.TimeoutSeconds = int.TryParse(config["Ldap:TimeoutSeconds"], out var t) ? t : 10;
            });
            services.AddScoped<IIdentityProvider, LdapIdentityProvider>();
        }
        else if (providerType == "Keycloak")
        {
            services.Configure<KeycloakSettings>(config.GetSection("Keycloak"));
            services.AddHttpClient("keycloak");
            services.AddScoped<IIdentityProvider, KeycloakIdentityProvider>();
        }
        else
        {
            services.AddScoped<IIdentityProvider, MockIdentityProvider>();
        }

        return services;
    }
}
