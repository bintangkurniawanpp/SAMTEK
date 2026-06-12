using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMTEK.Application.DTOs;
using SAMTEK.Application.Interfaces;
using System.Security.Claims;

namespace SAMTEK.Web.Controllers;

[ApiController]
[Route("api/mappings")]
[Authorize(Roles = "Admin")]
public class MappingController(
    IMappingService mappingService,
    IAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await mappingService.GetAllMappingsAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Assign([FromBody] AssignRoleDto dto, CancellationToken ct)
    {
        var admin = User.FindFirstValue(ClaimTypes.Name)!;
        var result = await mappingService.AssignRoleAsync(dto, admin, ct);
        await audit.LogAsync(admin, "ROLE_ASSIGNED", $"{dto.Username} → ArisRole {dto.ArisRoleId} / APPA {dto.AppaRole}", null, ct);
        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var admin = User.FindFirstValue(ClaimTypes.Name)!;
        await mappingService.ApproveMappingAsync(id, admin, ct);
        await audit.LogAsync(admin, "ROLE_APPROVED", $"MappingId={id}", null, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id, CancellationToken ct)
    {
        var admin = User.FindFirstValue(ClaimTypes.Name)!;
        await mappingService.RemoveMappingAsync(id, admin, ct);
        await audit.LogAsync(admin, "ROLE_REMOVED", $"MappingId={id}", null, ct);
        return NoContent();
    }
}
