using SAMTEK.Application.DTOs;

namespace SAMTEK.Application.Interfaces;

public interface IMappingService
{
    Task<UserCredentialsDto?> GetCredentialsAsync(string username, CancellationToken ct = default);
    Task<List<MappingDto>> GetAllMappingsAsync(CancellationToken ct = default);
    Task<MappingDto> AssignRoleAsync(AssignRoleDto dto, string assignedBy, CancellationToken ct = default);
    Task ApproveMappingAsync(int mappingId, string approvedBy, CancellationToken ct = default);
    Task RemoveMappingAsync(int mappingId, string removedBy, CancellationToken ct = default);
    Task UpdateLastLoginAsync(string username, CancellationToken ct = default);
}
