namespace SAMTEK.Application.Interfaces;

public interface IAdminAuthService
{
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken ct = default);
    Task<bool> HasAnyAdminAsync(CancellationToken ct = default);
    Task CreateAdminAsync(string username, string password, CancellationToken ct = default);
}
