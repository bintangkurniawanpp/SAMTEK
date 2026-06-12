using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SAMTEK.Application.DTOs;
using SAMTEK.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SAMTEK.Web.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IIdentityProvider identityProvider,
    IMappingService mappingService,
    IAuditService audit,
    IConfiguration config,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>POST /api/auth/login — validate credentials, return JWT + APPA role.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();

        var authResult = await identityProvider.AuthenticateAsync(req.Username, req.Password, ct);
        if (!authResult.Success)
        {
            await audit.LogAsync(req.Username, "LOGIN_FAIL", authResult.ErrorMessage, ip, ct);
            return Unauthorized(new { message = authResult.ErrorMessage ?? "Invalid credentials." });
        }

        var credentials = await mappingService.GetCredentialsAsync(authResult.Username!, ct);
        if (credentials == null)
        {
            await audit.LogAsync(authResult.Username!, "LOGIN_NO_MAPPING", "No approved role mapping", ip, ct);
            return Forbid(); // authenticated but no role assigned
        }

        await audit.LogAsync(authResult.Username!, "LOGIN_SUCCESS", credentials.AppaRole, ip, ct);
        await mappingService.UpdateLastLoginAsync(authResult.Username!, ct);

        var token = GenerateJwt(authResult.Username!, authResult.DisplayName!, credentials.AppaRole);
        return Ok(new LoginResponseDto(
            Token: token,
            Username: authResult.Username!,
            DisplayName: authResult.DisplayName!,
            AppaRole: credentials.AppaRole,
            ExpiresAt: DateTime.UtcNow.AddHours(8)));
    }

    /// <summary>GET /api/auth/credentials — return ARIS shared account credentials for the caller.</summary>
    [HttpGet("credentials")]
    [Authorize]
    public async Task<IActionResult> GetCredentials(CancellationToken ct)
    {
        var username = User.FindFirstValue(ClaimTypes.Name)!;
        var credentials = await mappingService.GetCredentialsAsync(username, ct);
        if (credentials == null)
            return NotFound(new { message = "No approved role mapping found for this user." });

        return Ok(credentials);
    }

    /// <summary>GET /api/auth/appa-role — return the caller's APPA role (Designer|Viewer).</summary>
    [HttpGet("appa-role")]
    [Authorize]
    public async Task<IActionResult> GetAppaRole(CancellationToken ct)
    {
        var username = User.FindFirstValue(ClaimTypes.Name)!;
        var credentials = await mappingService.GetCredentialsAsync(username, ct);
        if (credentials == null)
            return NotFound(new { message = "No approved role mapping found." });

        return Ok(new { username, appaRole = credentials.AppaRole });
    }

    private string GenerateJwt(string username, string displayName, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.")));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim("display_name", displayName),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"] ?? "SAMTEK",
            audience: config["Jwt:Audience"] ?? "SAMTEK",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
