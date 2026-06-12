using SAMTEK.Domain.Enums;

namespace SAMTEK.Application.DTOs;

public record UserCredentialsDto(
    string ArisUsername,
    string ArisPasswordBase64,      // Base64-encoded — same pattern as SAMTEK Java
    string AppaRole,                // "Designer" | "Viewer"
    bool CanPrint,
    bool CanDownload);

public record MappingDto(
    int Id,
    string Username,
    string DisplayName,
    string ArisRoleCode,
    string ArisUsername,
    AppRole AppaRole,
    bool IsApproved,
    string AssignedBy,
    DateTime AssignedAt,
    string? Notes,
    DateTime? LastLoginAt);

public class AssignRoleDto
{
    public string Username { get; set; } = "";
    public int ArisRoleId { get; set; }
    public AppRole AppaRole { get; set; }
    public string? Notes { get; set; }
}

public record ArisRoleDto(int Id, string RoleCode, string ArisUsername, string RoleType, bool CanPrint, bool CanDownload, bool IsActive);
